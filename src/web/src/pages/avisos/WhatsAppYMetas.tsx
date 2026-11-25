import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { api, unwrap, type Schemas } from '../../api/client';
import { useSession } from '../../session/Session';
import { Checkbox, Dialog, Field, PageHeader, QueryState, formatDateTime, useSave } from '../../components/Common';

type Aviso = Schemas['AvisoDto'];
type Plantilla = Schemas['PlantillaDto'];
type Tab = 'por-mandar' | 'historial' | 'plantillas' | 'metas';

export const avisosKey = ['avisos'] as const;
const plantillasKey = ['plantillas'] as const;

export const AVISO_STATE: Record<Schemas['AvisoState'], { text: string; tone: string }> = {
  Pendiente: { text: 'Por enviar', tone: '' },
  Enviado: { text: 'Enviado', tone: 'success' },
  Simulado: { text: 'Simulado (prueba)', tone: '' },
  ParaMandarAMano: { text: 'Por mandar a mano', tone: 'warning' },
  MandadoAMano: { text: 'Mandado a mano', tone: 'success' },
  Fallido: { text: 'Falló', tone: 'danger' },
  Omitido: { text: 'No se avisó', tone: '' },
};
export const CANAL: Record<NonNullable<Schemas['AvisoCanal']>, string> = { Push: 'Push', WhatsApp: 'WhatsApp' };

/**
 * WhatsApp y metas (docs/pantallas/oficina/whatsapp-y-metas.md): the avisos to send by hand while WhatsApp is not
 * connected, the log and the templates per event. Which aviso goes and by which channel is decided by the API.
 */
export function WhatsAppYMetas() {
  const [tab, setTab] = useState<Tab>('por-mandar');
  const avisos = useQuery({
    queryKey: [...avisosKey, tab],
    queryFn: () => unwrap(api.GET('/avisos', { params: { query: { estado: tab === 'por-mandar' ? 'ParaMandarAMano' : undefined } } })),
    enabled: tab === 'por-mandar' || tab === 'historial',
    refetchInterval: 30_000,
  });
  const toSend = avisos.data?.toSendByHand;

  return (
    <>
      <PageHeader
        title="WhatsApp y metas"
        description="Avisos al invitado por evento: push si tiene la app; si no, WhatsApp si lo acepta. Las metas de los KPIs llegan con KPIs."
      />
      {avisos.data && !avisos.data.whatsAppConnected && (
        <div className="alert" style={{ marginBottom: 16 }}>
          WhatsApp todavía no está conectado (faltan la cuenta de Meta y las plantillas aprobadas): cada aviso queda en "Por mandar" y se manda con un
          clic desde WhatsApp Web. {!avisos.data.pushConnected && 'Los avisos push se registran sin enviarse (modo de prueba).'}
        </div>
      )}
      <nav className="tabs">
        {(
          [
            ['por-mandar', `Por mandar${toSend ? ` (${toSend})` : ''}`],
            ['historial', 'Historial'],
            ['plantillas', 'Plantillas'],
            ['metas', 'Metas'],
          ] as [Tab, string][]
        ).map(([key, name]) => (
          <button key={key} type="button" className={tab === key ? 'selected' : ''} onClick={() => setTab(key)}>
            {name}
          </button>
        ))}
      </nav>
      {(tab === 'por-mandar' || tab === 'historial') && (
        <QueryState loading={avisos.isPending} error={avisos.error}>
          <AvisosTable avisos={avisos.data?.avisos ?? []} toSend={tab === 'por-mandar'} />
        </QueryState>
      )}
      {tab === 'plantillas' && <Plantillas />}
      {tab === 'metas' && (
        <div className="alert">Las metas de los KPIs (rojo, verde y súper verde por indicador) llegan con la pantalla KPIs en la semana 7.</div>
      )}
    </>
  );
}

function AvisosTable({ avisos, toSend }: { avisos: Aviso[]; toSend: boolean }) {
  const { can } = useSession();
  const canSend = can('whatsapp-y-metas', 'edit') || can('pedidos', 'edit');
  const sent = useSave((id: number) => unwrap(api.POST('/avisos/{id}/mandado', { params: { path: { id } } })), {
    refetch: [avisosKey],
    message: 'Aviso marcado como mandado.',
  });
  return (
    <section className="card">
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Invitado</th>
              <th>Aviso</th>
              <th>Texto</th>
              <th>Canal</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {avisos.length === 0 && (
              <tr>
                <td colSpan={7} className="muted">
                  {toSend ? 'No hay avisos por mandar.' : 'Todavía no hay avisos.'}
                </td>
              </tr>
            )}
            {avisos.map((a) => (
              <tr key={a.id}>
                <td>{formatDateTime(a.createdAt)}</td>
                <td>
                  <Link to={`/clientes?invitado=${a.invitadoId}`}>{a.invitadoName}</Link>
                  {a.phone && <div className="muted">{a.phone}</div>}
                </td>
                <td>
                  {a.eventoName}
                  {a.pedidoId && (
                    <div>
                      <Link to={`/pedidos?pedido=${a.pedidoId}`}>Pedido {a.pedidoId}</Link>
                    </div>
                  )}
                </td>
                <td style={{ maxWidth: 380 }}>
                  {a.title && <strong>{a.title} · </strong>}
                  {a.text}
                  {a.detail && <div className="muted">{a.detail}</div>}
                </td>
                <td>{a.canal ? CANAL[a.canal] : '—'}</td>
                <td>
                  <span className={`badge ${AVISO_STATE[a.state].tone}`}>{AVISO_STATE[a.state].text}</span>
                  {a.sentAt && <div className="muted">{formatDateTime(a.sentAt)}</div>}
                </td>
                <td className="end">
                  <SendByHand aviso={a} canSend={canSend} busy={sent.isPending} onSent={(id) => sent.mutate(id)} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {sent.error && <div className="alert error">{sent.error.message}</div>}
    </section>
  );
}

/** "Abrir WhatsApp" with the text written, then "Ya lo mandé". */
export function SendByHand({ aviso, canSend, busy, onSent }: { aviso: Aviso; canSend: boolean; busy: boolean; onSent: (id: number) => void }) {
  if (aviso.state !== 'ParaMandarAMano' || !aviso.whatsAppLink) return null;
  return (
    <span className="actions tight" style={{ justifyContent: 'flex-end' }}>
      <a className="button small primary" href={aviso.whatsAppLink} target="_blank" rel="noreferrer">
        Abrir WhatsApp
      </a>
      {canSend && (
        <button type="button" className="small" disabled={busy} onClick={() => onSent(aviso.id)}>
          Ya lo mandé
        </button>
      )}
    </span>
  );
}

function Plantillas() {
  const canEdit = useSession().can('whatsapp-y-metas', 'edit');
  const [editing, setEditing] = useState<Plantilla | null>(null);
  const list = useQuery({ queryKey: plantillasKey, queryFn: () => unwrap(api.GET('/plantillas')) });
  return (
    <QueryState loading={list.isPending} error={list.error}>
      <section className="card">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Evento</th>
                <th>Plantilla en Meta</th>
                <th>WhatsApp</th>
                <th>Push</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {list.data?.map((p) => (
                <tr key={p.id} className={p.active ? '' : 'inactive'}>
                  <td>
                    <strong>{p.eventoName}</strong>
                    <div className="muted">{p.variables.map((v, i) => `{{${i + 1}}} ${v}`).join(' · ')}</div>
                  </td>
                  <td>
                    <code>{p.metaName}</code>
                  </td>
                  <td style={{ maxWidth: 360 }}>{p.whatsAppText}</td>
                  <td style={{ maxWidth: 260 }}>
                    <strong>{p.pushTitle}</strong>
                    <div>{p.pushText}</div>
                  </td>
                  <td>
                    <span className={`badge ${p.active ? 'success' : ''}`}>{p.active ? 'Activa' : 'Inactiva'}</span>
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(p)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      {editing && <PlantillaForm plantilla={editing} onClose={() => setEditing(null)} />}
    </QueryState>
  );
}

function PlantillaForm({ plantilla: p, onClose }: { plantilla: Plantilla; onClose: () => void }) {
  const [metaName, setMetaName] = useState(p.metaName);
  const [whatsAppText, setWhatsAppText] = useState(p.whatsAppText);
  const [pushTitle, setPushTitle] = useState(p.pushTitle);
  const [pushText, setPushText] = useState(p.pushText);
  const [active, setActive] = useState(p.active);
  const body = { metaName, whatsAppText, pushTitle, pushText, active };
  const save = useSave(() => unwrap(api.PUT('/plantillas/{id}', { params: { path: { id: p.id } }, body })), {
    refetch: [plantillasKey],
    message: 'Plantilla guardada.',
    onDone: onClose,
  });
  const preview = useQuery({
    queryKey: [...plantillasKey, p.id, 'preview', whatsAppText, pushTitle, pushText],
    queryFn: () => unwrap(api.POST('/plantillas/{id}/previsualizar', { params: { path: { id: p.id } }, body })),
    placeholderData: (previous) => previous,
  });
  const variables = p.variables.map((v, i) => `{{${i + 1}}} ${v}`).join(' · ');

  return (
    <Dialog title={`Plantilla · ${p.eventoName}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Nombre en Meta" hint="El nombre de la plantilla aprobada en Meta." wide>
          <input value={metaName} onChange={(e) => setMetaName(e.target.value)} required maxLength={100} />
        </Field>
        <Field label="Texto de WhatsApp" hint={`Variables: ${variables}.`} wide>
          <textarea value={whatsAppText} onChange={(e) => setWhatsAppText(e.target.value)} required maxLength={1024} rows={3} />
        </Field>
        <Field label="Título del push">
          <input value={pushTitle} onChange={(e) => setPushTitle(e.target.value)} required maxLength={65} />
        </Field>
        <Field label="Texto del push">
          <input value={pushText} onChange={(e) => setPushText(e.target.value)} required maxLength={240} />
        </Field>
        <Checkbox checked={active} onChange={setActive} wide>
          Activa (se avisa este evento)
        </Checkbox>
        <div className="field wide">
          <span>Vista previa con datos de ejemplo</span>
          {preview.error ? (
            <div className="alert error">{preview.error.message}</div>
          ) : (
            <div className="alert">
              <div>
                <strong>WhatsApp:</strong> {preview.data?.whatsApp}
              </div>
              <div>
                <strong>Push:</strong> {preview.data?.pushTitle} · {preview.data?.pushText}
              </div>
            </div>
          )}
        </div>
      </div>
    </Dialog>
  );
}

import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useRutas } from '../../api/queries';
import { useSession } from '../../session/Session';
import { Checkbox, Dialog, Field, PageHeader, QueryState, formatDate, formatDateTime, optional, useSave } from '../../components/Common';

type Row = Schemas['CrmRowDto'];
type Status = Schemas['CrmStatus'];
type Result = Schemas['ContactResult'];

export const STATUS: Record<Status, { name: string; tone: string }> = {
  LeToca: { name: 'Le toca', tone: 'warning' },
  EnRiesgo: { name: 'En riesgo', tone: 'danger' },
  Nuevo: { name: 'Nuevo', tone: 'success' },
  Activo: { name: 'Activo', tone: 'brand' },
  Prospecto: { name: 'Prospecto', tone: '' },
  Inactivo: { name: 'Inactivo', tone: '' },
};
const STATUS_ORDER: Status[] = ['LeToca', 'EnRiesgo', 'Nuevo', 'Activo', 'Inactivo', 'Prospecto'];

const RESULTS: { key: Result; name: string; hint: string }[] = [
  { key: 'NoContesto', name: 'No contestó', hint: 'Se vuelve a llamar mañana' },
  { key: 'LlamarDespues', name: 'Pidió que le llamaran', hint: 'En 3 días' },
  { key: 'NoPorAhora', name: 'No por ahora', hint: 'En 3 semanas' },
  { key: 'Agendo', name: 'Agendó', hint: 'Sin seguimiento' },
  { key: 'NoContactar', name: 'No contactar', hint: 'Sale de "Llamar hoy" y campañas' },
];
const resultName = (r: Result) => RESULTS.find((x) => x.key === r)?.name ?? r;

function StatusBadge({ status, manual }: { status: Status; manual?: boolean }) {
  const s = STATUS[status];
  return (
    <span className={`badge ${s.tone}`} title={manual ? 'Fijado por oficina' : undefined}>
      {s.name}
      {manual && ' ✎'}
    </span>
  );
}

const shortDate = new Intl.DateTimeFormat('es-MX', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
const short = (iso?: string | null) => (iso ? shortDate.format(new Date(`${iso}T00:00:00Z`)).replace('.', '') : '—');

/**
 * Clientes (CRM): status of each invitado, "Llamar hoy", recording contacts and the ficha
 * (docs/pantallas/oficina/clientes-crm.md). Status and lists come calculated from the API.
 */
export function Clientes() {
  const canEdit = useSession().can('clientes-crm', 'edit');
  const [tab, setTab] = useState<'llamar' | 'todos'>('llamar');
  const [status, setStatus] = useState<Status | null>(null);
  const [rutaId, setRutaId] = useState<number | null>(null);
  const [text, setText] = useState('');
  const [q, setQ] = useState('');
  const [contacting, setContacting] = useState<Row | null>(null);
  const [ficha, setFicha] = useState<number | null>(null);
  const rutas = useRutas();

  useEffect(() => {
    const t = setTimeout(() => setQ(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);

  const list = useQuery({
    queryKey: [...keys.crm, tab, status, rutaId, q],
    queryFn: () =>
      tab === 'llamar'
        ? unwrap(api.GET('/invitados/llamar-hoy', { params: { query: { ruta: rutaId ?? undefined, q } } }))
        : unwrap(api.GET('/invitados', { params: { query: { estatus: status ?? undefined, ruta: rutaId ?? undefined, q } } })),
    placeholderData: (previous) => previous,
  });
  const d = list.data;

  return (
    <div className="stack">
      <PageHeader title="Clientes (CRM)" description="Estatus de cada invitado por su frecuencia real y la lista de a quién llamar un día antes de que pase su ruta.">
        <Link to="/agendar" className="button primary">
          Nuevo cliente
        </Link>
      </PageHeader>

      {d && (
        <div className="crm-counts">
          {STATUS_ORDER.map((s) => {
            const count = d.counts.find((c) => c.status === s)?.count ?? 0;
            return (
              <button
                key={s}
                type="button"
                className={`crm-count${status === s && tab === 'todos' ? ' selected' : ''}`}
                onClick={() => {
                  setTab('todos');
                  setStatus(status === s ? null : s);
                }}
              >
                <strong>{count}</strong>
                <StatusBadge status={s} />
              </button>
            );
          })}
          <span className="muted">de {d.total} invitados (sin tiendas)</span>
        </div>
      )}

      <section className="card">
        <div className="card__header">
          <div className="segmented">
            <button type="button" className={`segmented__option${tab === 'llamar' ? ' selected' : ''}`} onClick={() => setTab('llamar')}>
              Llamar hoy{d ? ` (${d.callTodayCount})` : ''}
            </button>
            <button
              type="button"
              className={`segmented__option${tab === 'todos' ? ' selected' : ''}`}
              onClick={() => {
                setTab('todos');
                setStatus(null);
              }}
            >
              Todos
            </button>
          </div>
          <div className="actions">
            <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Nombre, colonia o teléfono" style={{ maxWidth: 240 }} />
            <select value={rutaId ?? ''} onChange={(e) => setRutaId(e.target.value ? Number(e.target.value) : null)} style={{ width: 'auto' }}>
              <option value="">Todas las rutas</option>
              {rutas.data
                ?.filter((r) => r.active)
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    Ruta {r.code}
                  </option>
                ))}
            </select>
          </div>
        </div>
        {tab === 'llamar' && (
          <p className="muted crm-hint">Le toca o en riesgo, cuya ruta pasa mañana por su zona; sin fecha de contacto futura y sin "no contactar".</p>
        )}
        <QueryState loading={list.isPending} error={list.error}>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Invitado</th>
                  <th>Zona</th>
                  <th>Estatus</th>
                  <th>Por qué</th>
                  <th>Visita sugerida</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {d?.rows.map((r) => (
                  <tr key={r.id}>
                    <td>
                      <button type="button" className="link" onClick={() => setFicha(r.id)}>
                        <strong>{r.name}</strong>
                      </button>
                      <div className="muted">
                        {r.whatsAppPhone}
                        {r.neighborhood && ` · ${r.neighborhood}`}
                      </div>
                    </td>
                    <td>
                      {r.zonaCode ?? <span className="badge warning">Sin zona</span>}
                      {r.rutaCode && <div className="muted">Ruta {r.rutaCode}</div>}
                    </td>
                    <td>
                      <StatusBadge status={r.status} manual={r.statusIsManual} />
                      {r.doNotContact && <div className="badge danger crm-flag">No contactar</div>}
                    </td>
                    <td className="crm-why">
                      {r.why}
                      {r.nextRecoleccion && <div className="muted">Ya tiene recolección el {short(r.nextRecoleccion)}</div>}
                      {r.nextContactOn && <div className="muted">Volver a contactar: {short(r.nextContactOn)}</div>}
                    </td>
                    <td>{short(r.suggestedVisit)}</td>
                    <td className="end">
                      <div className="actions tight end">
                        <a className="button small" href={`https://wa.me/52${r.whatsAppPhone}`} target="_blank" rel="noreferrer">
                          WhatsApp
                        </a>
                        {canEdit && (
                          <button type="button" className="small" onClick={() => setContacting(r)}>
                            Registrar
                          </button>
                        )}
                        <Link className="button small" to={`/agendar?invitado=${r.id}`}>
                          Agendar
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {d?.rows.length === 0 && (
              <div className="empty">{tab === 'llamar' ? 'Nadie por llamar hoy.' : 'Sin invitados con estos filtros.'}</div>
            )}
          </div>
        </QueryState>
      </section>

      {contacting && <ContactForm invitadoId={contacting.id} name={contacting.name} onClose={() => setContacting(null)} />}
      {ficha !== null && <Ficha id={ficha} onClose={() => setFicha(null)} onContact={(r) => setContacting(r)} />}
    </div>
  );
}

function ContactForm({ invitadoId, name, onClose }: { invitadoId: number; name: string; onClose: () => void }) {
  const [channel, setChannel] = useState<Schemas['ContactChannel']>('Llamada');
  const [result, setResult] = useState<Result>('NoContesto');
  const [note, setNote] = useState('');
  const [next, setNext] = useState('');
  const save = useSave(
    () =>
      unwrap(
        api.POST('/invitados/{id}/contactos', {
          params: { path: { id: invitadoId } },
          body: { channel, result, note: optional(note), nextContactOn: next || null },
        }),
      ),
    { refetch: [keys.crm], message: 'Contacto registrado.', onDone: onClose },
  );
  return (
    <Dialog title={`Registrar contacto con ${name}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <Field label="Canal">
        <div className="segmented">
          {(['Llamada', 'WhatsApp'] as const).map((c) => (
            <button key={c} type="button" className={`segmented__option${channel === c ? ' selected' : ''}`} onClick={() => setChannel(c)}>
              {c}
            </button>
          ))}
        </div>
      </Field>
      <Field label="Resultado">
        <select value={result} onChange={(e) => setResult(e.target.value as Result)}>
          {RESULTS.map((r) => (
            <option key={r.key} value={r.key}>
              {r.name} — {r.hint}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Volver a contactar el" hint="Vacío = según el resultado.">
        <input type="date" value={next} onChange={(e) => setNext(e.target.value)} />
      </Field>
      <Field label="Nota">
        <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={3} maxLength={1000} />
      </Field>
    </Dialog>
  );
}

function Ficha({ id, onClose, onContact }: { id: number; onClose: () => void; onContact: (r: Row) => void }) {
  const canEdit = useSession().can('clientes-crm', 'edit');
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);
  const ficha = useQuery({
    queryKey: [...keys.crm, 'ficha', id],
    queryFn: () => unwrap(api.GET('/invitados/{id}/ficha', { params: { path: { id } } })),
  });
  const f = ficha.data;

  return (
    <dialog ref={ref} onClose={onClose} className="ficha">
      <div className="dialog__header">
        <h2>{f?.invitado.name ?? 'Ficha'}</h2>
        {f && (
          <p className="muted">
            WhatsApp {f.invitado.whatsAppPhone}
            {f.invitado.howFoundUs && ` · ${f.invitado.howFoundUs}`}
          </p>
        )}
      </div>
      <div className="dialog__body stack">
        <QueryState loading={ficha.isPending} error={ficha.error}>
          {f && (
            <>
              <div className="ficha__stats">
                <div>
                  <span className="muted">Estatus</span>
                  <StatusBadge status={f.crm.status} manual={f.crm.statusIsManual} />
                </div>
                <div>
                  <span className="muted">Servicios</span>
                  <strong>{f.crm.serviceCount}</strong>
                </div>
                <div>
                  <span className="muted">Frecuencia</span>
                  <strong>{f.crm.frequencyDays ? `cada ${f.crm.frequencyDays} días` : '—'}</strong>
                </div>
                <div>
                  <span className="muted">Le toca</span>
                  <strong>{short(f.crm.leTocaDate)}</strong>
                </div>
                <div>
                  <span className="muted">Último servicio</span>
                  <strong>{short(f.crm.lastService)}</strong>
                </div>
              </div>
              <p className="muted">{f.crm.why}</p>

              <div className="ficha__grid">
                <section className="stack">
                  <h3>Direcciones</h3>
                  {f.invitado.direcciones.length === 0 && <div className="alert">Sin direcciones.</div>}
                  {f.invitado.direcciones.map((dir) => (
                    <div key={dir.id} className="address-option">
                      <span>
                        {dir.label && <strong>{dir.label}: </strong>}
                        {dir.street} {dir.exteriorNumber}
                        {dir.neighborhood && `, ${dir.neighborhood}`} · CP {dir.postalCode}
                      </span>
                      {dir.zonaCode ? <span className="badge brand">Zona {dir.zonaCode}</span> : <span className="badge warning">Sin zona</span>}
                    </div>
                  ))}
                  {canEdit && <Seguimiento ficha={f} />}
                </section>

                <section className="stack">
                  <div className="actions" style={{ justifyContent: 'space-between' }}>
                    <h3>Contactos</h3>
                    {canEdit && (
                      <button type="button" className="small" onClick={() => onContact(f.crm)}>
                        Registrar contacto
                      </button>
                    )}
                  </div>
                  {f.contactos.length === 0 && <span className="muted">Sin contactos registrados.</span>}
                  <ul className="timeline">
                    {f.contactos.map((c) => (
                      <li key={c.id}>
                        <strong>
                          {c.channel} · {resultName(c.result)}
                        </strong>
                        <span className="muted">
                          {formatDateTime(c.contactedAt)}
                          {c.nextContactOn && ` · volver el ${short(c.nextContactOn)}`}
                        </span>
                        {c.note && <span>{c.note}</span>}
                      </li>
                    ))}
                  </ul>

                  <h3>Pedidos</h3>
                  {f.pedidos.length === 0 && <span className="muted">Sin pedidos.</span>}
                  <ul className="timeline">
                    {f.pedidos.slice(0, 20).map((p) => (
                      <li key={p.id}>
                        <strong>{formatDate(p.recoleccionDate)}</strong>
                        <span className="muted">
                          {p.etapa} · Ruta {p.rutaCode ?? '—'} · {p.lineasServicio.join(', ') || 'Sin línea'}
                        </span>
                      </li>
                    ))}
                  </ul>
                  {f.pedidos.length > 20 && <span className="muted">y {f.pedidos.length - 20} más.</span>}
                </section>
              </div>
            </>
          )}
        </QueryState>
      </div>
      <div className="dialog__footer">
        {f && (
          <Link className="button" to={`/agendar?invitado=${f.invitado.id}`}>
            Agendar recolección
          </Link>
        )}
        <button type="button" className="primary" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </dialog>
  );
}

function Seguimiento({ ficha }: { ficha: Schemas['CrmFichaDto'] }) {
  const [manual, setManual] = useState<Status | ''>(ficha.manualStatus ?? '');
  const [frequency, setFrequency] = useState(ficha.fixedFrequencyDays?.toString() ?? '');
  const [next, setNext] = useState(ficha.crm.nextContactOn ?? '');
  const [doNotContact, setDoNotContact] = useState(ficha.crm.doNotContact);
  const save = useSave(
    () =>
      unwrap(
        api.PUT('/invitados/{id}/seguimiento', {
          params: { path: { id: ficha.invitado.id } },
          body: {
            manualStatus: manual || null,
            fixedFrequencyDays: frequency ? Number(frequency) : null,
            nextContactOn: next || null,
            doNotContact,
          },
        }),
      ),
    { refetch: [keys.crm], message: 'Seguimiento guardado.' },
  );
  return (
    <form
      className="stack card__body seguimiento"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate(undefined);
      }}
    >
      <h3>Seguimiento</h3>
      <div className="form-grid">
        <Field label="Estatus" hint="Vacío = automático">
          <select value={manual} onChange={(e) => setManual(e.target.value as Status | '')}>
            <option value="">Automático</option>
            {STATUS_ORDER.map((s) => (
              <option key={s} value={s}>
                {STATUS[s].name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Frecuencia fija (días)" hint="Vacío = calculada">
          <input type="number" min={1} max={365} value={frequency} onChange={(e) => setFrequency(e.target.value)} />
        </Field>
        <Field label="Próximo contacto">
          <input type="date" value={next} onChange={(e) => setNext(e.target.value)} />
        </Field>
      </div>
      <Checkbox checked={doNotContact} onChange={setDoNotContact}>
        No contactar (pidió no recibir llamadas ni campañas)
      </Checkbox>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <div className="actions end">
        <button type="submit" className="primary" disabled={save.isPending}>
          {save.isPending ? 'Guardando…' : 'Guardar seguimiento'}
        </button>
      </div>
    </form>
  );
}

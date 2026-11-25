import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import type { Plantilla } from '../types/avisos';
import { plantillasKey } from '../services/queryKeys';
import '../../../shared/api/client';
import { QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { avisosApi } from '../services/avisosApi';

export function Plantillas() {
  const canEdit = useSession().can('whatsapp-y-metas', 'edit');
  const [editing, setEditing] = useState<Plantilla | null>(null);
  const list = useQuery({ queryKey: plantillasKey, queryFn: () => avisosApi.listPlantillas() });
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
  const save = useSave(() => avisosApi.updatePlantilla(p.id, body), {
    refetch: [plantillasKey],
    message: 'Plantilla guardada.',
    onDone: onClose,
  });
  const preview = useQuery({
    queryKey: [...plantillasKey, p.id, 'preview', whatsAppText, pushTitle, pushText],
    queryFn: () => avisosApi.previewPlantilla(p.id, body),
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

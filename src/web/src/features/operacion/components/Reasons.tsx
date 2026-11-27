import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useCanEdit } from '../hooks/useCanEdit';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { type Reason, REASON_TYPES } from '../types/catalogs';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function Reasons() {
  const canEdit = useCanEdit();
  const reasons = useQuery({ queryKey: keys.reasons, queryFn: () => operacionApi.listMotivos() });
  const [editing, setEditing] = useState<Reason | 'new' | null>(null);

  const pagedReasons = usePaged(reasons.data);
  return (
    <section className="card">
      <div className="card__header">
        <h2>Motivos</h2>
        {canEdit && (
          <button type="button" onClick={() => setEditing('new')}>
            Nuevo motivo
          </button>
        )}
      </div>
      <QueryState loading={reasons.isPending} error={reasons.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Texto</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pagedReasons.rows.map((m) => (
                <tr key={m.id} className={m.active ? '' : 'inactive'}>
                  <td>{REASON_TYPES[m.type]}</td>
                  <td>
                    {m.text}
                    {m.isDigital && <span className="badge brand"> Marketing digital</span>}
                  </td>
                  <td>
                    <ActiveBadge active={m.active} />
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(m)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {reasons.data?.length === 0 && <div className="empty">Sin motivos.</div>}
        </div>
        <Pagination {...pagedReasons} />
      </QueryState>
      {editing && <ReasonForm reason={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function ReasonForm({ reason, onClose }: { reason: Reason | null; onClose: () => void }) {
  const [type, setType] = useState<Reason['type']>(reason?.type ?? 'FailedEntrega');
  const [text, setText] = useState(reason?.text ?? '');
  const [active, setActive] = useState(reason?.active ?? true);
  const [isDigital, setIsDigital] = useState(reason?.isDigital ?? false);
  const save = useSave(
    () => {
      const body = { type, text: text.trim(), active, isDigital: type === 'HowFoundUs' && isDigital };
      return reason ? operacionApi.updateMotivo(reason.id, body) : operacionApi.createMotivo(body);
    },
    { refetch: [keys.reasons], message: 'Motivo guardado.', onDone: onClose },
  );
  return (
    <Dialog
      title={reason ? 'Editar motivo' : 'Nuevo motivo'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Tipo">
        <select value={type} onChange={(e) => setType(e.target.value as Reason['type'])}>
          {Object.entries(REASON_TYPES).map(([key, name]) => (
            <option key={key} value={key}>
              {name}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Texto">
        <input autoFocus value={text} onChange={(e) => setText(e.target.value)} required />
      </Field>
      {type === 'HowFoundUs' && (
        <Checkbox checked={isDigital} onChange={setIsDigital}>
          Es marketing digital (cuenta en "nuevos de marketing digital" de Clientes y fuentes)
        </Checkbox>
      )}
      <Checkbox checked={active} onChange={setActive}>
        Activo
      </Checkbox>
    </Dialog>
  );
}

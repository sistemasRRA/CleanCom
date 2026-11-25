import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useCanEdit } from '../hooks/useCanEdit';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import type { ParadaType } from '../types/catalogs';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function ParadaTypes() {
  const canEdit = useCanEdit();
  const types = useQuery({ queryKey: keys.paradaTypes, queryFn: () => operacionApi.listParadaTypes() });
  const [editing, setEditing] = useState<ParadaType | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <h2>Tipos de parada</h2>
        {canEdit && (
          <button type="button" onClick={() => setEditing('new')}>
            Nuevo tipo
          </button>
        )}
      </div>
      <QueryState loading={types.isPending} error={types.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Campos requeridos</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {types.data?.map((t) => (
                <tr key={t.id} className={t.active ? '' : 'inactive'}>
                  <td>
                    <strong>{t.name}</strong>
                  </td>
                  <td className="muted">{t.requiredFields ?? '—'}</td>
                  <td>
                    <ActiveBadge active={t.active} />
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(t)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
      {editing && <ParadaTypeForm type={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function ParadaTypeForm({ type, onClose }: { type: ParadaType | null; onClose: () => void }) {
  const [name, setName] = useState(type?.name ?? '');
  const [fields, setFields] = useState(type?.requiredFields ?? '');
  const [active, setActive] = useState(type?.active ?? true);
  const save = useSave(
    () => {
      const body = { name: name.trim(), requiredFields: optional(fields), active };
      return type ? operacionApi.updateParadaType(type.id, body) : operacionApi.createParadaType(body);
    },
    { refetch: [keys.paradaTypes], message: 'Tipo de parada guardado.', onDone: onClose },
  );
  return (
    <Dialog
      title={type ? `Editar ${type.name}` : 'Nuevo tipo de parada'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Nombre">
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>
      <Field label="Campos requeridos" hint="Opcional.">
        <input value={fields} onChange={(e) => setFields(e.target.value)} />
      </Field>
      <Checkbox checked={active} onChange={setActive}>
        Activo
      </Checkbox>
    </Dialog>
  );
}

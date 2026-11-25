import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useCanEdit } from '../hooks/useCanEdit';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import type { LineaServicio } from '../types/catalogs';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { optionalNumber } from '../../../shared/utils/format';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function LineasServicio() {
  const canEdit = useCanEdit();
  const lineas = useQuery({ queryKey: keys.lineasServicio, queryFn: () => operacionApi.listLineasServicio() });
  const [editing, setEditing] = useState<LineaServicio | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Líneas de servicio</h2>
          <p className="muted">Días de proceso vacío = usa la regla general.</p>
        </div>
        {canEdit && (
          <button type="button" onClick={() => setEditing('new')}>
            Nueva línea
          </button>
        )}
      </div>
      <QueryState loading={lineas.isPending} error={lineas.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Días de proceso</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {lineas.data?.map((l) => (
                <tr key={l.id} className={l.active ? '' : 'inactive'}>
                  <td>
                    <strong>{l.name}</strong>
                  </td>
                  <td className="num">{l.processingDays ?? <span className="muted">general</span>}</td>
                  <td>
                    <ActiveBadge active={l.active} />
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(l)}>
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
      {editing && <LineaServicioForm linea={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function LineaServicioForm({ linea, onClose }: { linea: LineaServicio | null; onClose: () => void }) {
  const [name, setName] = useState(linea?.name ?? '');
  const [days, setDays] = useState(linea?.processingDays?.toString() ?? '');
  const [active, setActive] = useState(linea?.active ?? true);
  const save = useSave(
    () => {
      const body = { name: name.trim(), processingDays: optionalNumber(days), active };
      return linea ? operacionApi.updateLineaServicio(linea.id, body) : operacionApi.createLineaServicio(body);
    },
    { refetch: [keys.lineasServicio], message: 'Línea guardada.', onDone: onClose },
  );
  return (
    <Dialog
      title={linea ? `Editar ${linea.name}` : 'Nueva línea de servicio'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Nombre">
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>
      <Field label="Días de proceso">
        <input type="number" min={0} value={days} onChange={(e) => setDays(e.target.value)} placeholder="general" />
      </Field>
      <Checkbox checked={active} onChange={setActive}>
        Activa
      </Checkbox>
    </Dialog>
  );
}

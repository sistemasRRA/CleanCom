import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useCanEdit } from '../hooks/useCanEdit';
import { useConfirm } from '../../../shared/components/Toasts';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { type Lugar, LUGAR_TYPES } from '../types/catalogs';
import { useAction } from '../../../shared/hooks/useSave';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { LugarForm } from '../components/LugarForm';
import { operacionApi } from '../services/operacionApi';

export function Lugares() {
  const canEdit = useCanEdit();
  const ask = useConfirm();
  const lugares = useQuery({
    queryKey: keys.lugares,
    queryFn: () => operacionApi.listLugares({ includeInactive: true }),
  });
  const [editing, setEditing] = useState<Lugar | 'new' | null>(null);
  const deactivate = useAction((id: number) => operacionApi.removeLugar(id), [keys.lugares], 'Lugar desactivado.');

  const pagedLugares = usePaged(lugares.data);
  return (
    <section className="card">
      <div className="card__header">
        <h2>Tiendas y bodegas</h2>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nuevo lugar
          </button>
        )}
      </div>
      <QueryState loading={lugares.isPending} error={lugares.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Tipo</th>
                <th>Dirección</th>
                <th>Coordenadas</th>
                <th>Horario</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pagedLugares.rows.map((l) => (
                <tr key={l.id} className={l.active ? '' : 'inactive'}>
                  <td>
                    <strong>{l.name}</strong>
                  </td>
                  <td>{LUGAR_TYPES[l.type]}</td>
                  <td>{l.address}</td>
                  <td className="num muted">{l.lat != null && l.lng != null ? `${l.lat}, ${l.lng}` : '—'}</td>
                  <td className="muted">{l.openingHours ?? '—'}</td>
                  <td>
                    <ActiveBadge active={l.active} />
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        <button type="button" className="small" onClick={() => setEditing(l)}>
                          Editar
                        </button>
                        {l.active && (
                          <button
                            type="button"
                            className="small danger"
                            disabled={deactivate.isPending}
                            onClick={async () =>
                              (await ask(`¿Desactivar ${l.name}?`, { confirmText: 'Desactivar', danger: true })) && deactivate.mutate(l.id)
                            }
                          >
                            Desactivar
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination {...pagedLugares} />
      </QueryState>
      {editing && <LugarForm lugar={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

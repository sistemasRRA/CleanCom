import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useCanEdit } from '../hooks/useCanEdit';
import { useConfirm } from '../../../shared/components/Toasts';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { useAction } from '../../../shared/hooks/useSave';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { QueryState } from '../../../shared/components/Page';
import { formatDate } from '../../../shared/utils/dates';
import { NonWorkingDayForm } from '../components/NonWorkingDayForm';
import { operacionApi } from '../services/operacionApi';

// ---------------- Días inhábiles ----------------

export function NonWorkingDays() {
  const canEdit = useCanEdit();
  const ask = useConfirm();
  const days = useQuery({ queryKey: keys.nonWorkingDays, queryFn: () => operacionApi.listNonWorkingDays() });
  const [adding, setAdding] = useState(false);
  const remove = useAction((id: number) => operacionApi.removeNonWorkingDay(id), [keys.nonWorkingDays], 'Día inhábil quitado.');

  const pagedDays = usePaged(days.data);
  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Días inhábiles</h2>
          <p className="muted">No se agenda ni se rutea en estas fechas.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setAdding(true)}>
            Agregar día
          </button>
        )}
      </div>
      <QueryState loading={days.isPending} error={days.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Motivo</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pagedDays.rows.map((d) => (
                <tr key={d.id}>
                  <td>
                    <strong>{formatDate(d.date)}</strong>
                  </td>
                  <td>{d.reason}</td>
                  <td className="end">
                    {canEdit && (
                      <button
                        type="button"
                        className="small danger"
                        disabled={remove.isPending}
                        onClick={async () =>
                          (await ask(`¿Quitar el día inhábil ${formatDate(d.date)}?`, { confirmText: 'Quitar', danger: true })) && remove.mutate(d.id)
                        }
                      >
                        Quitar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {days.data?.length === 0 && <div className="empty">No hay días inhábiles registrados.</div>}
        </div>
        <Pagination {...pagedDays} />
      </QueryState>
      {adding && <NonWorkingDayForm onClose={() => setAdding(false)} />}
    </section>
  );
}

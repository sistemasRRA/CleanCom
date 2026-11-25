import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNotify } from '../../../shared/components/Toasts';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { useAction } from '../../../shared/hooks/useSave';
import { actionName, categoryOf, short, turnoName } from '../types/fijas';
import { addDays } from '../../../shared/utils/dates';
import { QueryState } from '../../../shared/components/Page';
import { paradasFijasApi } from '../services/paradasFijasApi';

/** "La semana de un vistazo": rutas by day, Monday to Saturday, with the cupo the fijas take. */
export function WeekGrid({ onOpen, canEdit }: { onOpen: (id: number) => void; canEdit: boolean }) {
  const [date, setDate] = useState<string | null>(null);
  const [regenerating, setRegenerating] = useState('');
  const notify = useNotify();
  const week = useQuery({
    queryKey: [...keys.paradasFijas, 'semana', date],
    queryFn: () => paradasFijasApi.week({ fecha: date ?? undefined }),
    placeholderData: (previous) => previous,
  });
  const regenerate = useAction(
    (fecha: string) =>
      paradasFijasApi.generate({ fecha }).then((r) => {
        notify(r.message);
        setRegenerating('');
      }),
    [keys.paradasFijas, keys.agendaBoard],
    'Listo.',
  );
  const w = week.data;

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>La semana de un vistazo</h2>
          {w && (
            <p className="muted">
              Del {short(w.from)} al {short(w.to)}. Cada tramo ocupa un lugar del cupo · 1° = primera de la ruta · Últ. = última · ☀ mañana · ☾ tarde
            </p>
          )}
        </div>
        <div className="actions">
          <button type="button" className="small" onClick={() => w && setDate(addDays(w.from, -7))}>
            ◀ Anterior
          </button>
          <button type="button" className="small" onClick={() => setDate(null)}>
            Esta semana
          </button>
          <button type="button" className="small" onClick={() => w && setDate(addDays(w.from, 7))}>
            Siguiente ▶
          </button>
          {canEdit && (
            <span className="actions tight">
              <input
                type="date"
                value={regenerating}
                onChange={(e) => setRegenerating(e.target.value)}
                style={{ width: 'auto' }}
                title="Día a regenerar"
              />
              <button
                type="button"
                className="small"
                disabled={!regenerating || regenerate.isPending}
                onClick={() => regenerate.mutate(regenerating)}
                title="Reemplaza las paradas fijas de ese día que el chofer no ha iniciado"
              >
                Agregar las fijas del día
              </button>
            </span>
          )}
        </div>
      </div>
      <QueryState loading={week.isPending} error={week.error}>
        {w && (
          <div className="table-wrap">
            <table className="fijas-week">
              <thead>
                <tr>
                  <th>Ruta</th>
                  {w.days.map((d) => (
                    <th key={d.date} className={d.nonWorkingReason ? 'off' : ''}>
                      {d.label}
                      {d.nonWorkingReason && <div className="muted">Inhábil</div>}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {w.rutas.map((r) => (
                  <tr key={r.id}>
                    <th>
                      <span className="ruta-color" style={{ background: r.color }} />
                      {r.code}
                    </th>
                    {w.days.map((d) => {
                      const cell = d.rutas.find((c) => c.rutaId === r.id);
                      return (
                        <td key={d.date} className={d.nonWorkingReason ? 'off' : ''}>
                          {d.nonWorkingReason ? (
                            <span className="muted">{d.nonWorkingReason}</span>
                          ) : (
                            <div className="fijas-cell">
                              {cell?.items.map((i) => {
                                // In a vuelta, "1°" goes on the first tramo only.
                                const mark =
                                  i.placement === 'Primera' && (i.step ?? 1) === 1
                                    ? '1° '
                                    : i.placement === 'Ultima' && (i.step ?? i.steps) === i.steps
                                      ? 'Últ. '
                                      : '';
                                const icon = i.turno === 'Manana' ? '☀ ' : i.turno === 'Tarde' ? '☾ ' : '';
                                return (
                                  <button
                                    key={`${i.paradaFijaId}-${i.step ?? 0}`}
                                    type="button"
                                    className={`fija-chip ${categoryOf(i.category).tone}`}
                                    onClick={() => onOpen(i.paradaFijaId)}
                                    title={[
                                      categoryOf(i.category).name,
                                      actionName(i.kind),
                                      turnoName(i.turno),
                                      i.before && `antes de las ${i.before.slice(0, 5)}`,
                                      i.activity,
                                      i.generated && 'ya está en la ruta',
                                    ]
                                      .filter(Boolean)
                                      .join(' · ')}
                                  >
                                    <b>
                                      {mark}
                                      {icon}
                                    </b>
                                    {i.name}
                                    {i.step != null && ` ${i.step}/${i.steps}`}
                                    {i.place && i.place !== i.name && <small> · {i.place}</small>}
                                  </button>
                                );
                              })}
                              {cell && (
                                <span className={`muted fijas-cupo${cell.fijasCupo > cell.cupo / 2 ? ' warn' : ''}`}>
                                  {cell.fijasCupo} de {cell.cupo} lugares
                                </span>
                              )}
                            </div>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </QueryState>
    </section>
  );
}

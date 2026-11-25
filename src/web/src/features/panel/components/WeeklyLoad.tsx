import { useNavigate } from 'react-router';
import { useMemo } from 'react';
import type { Panel } from '../types/panel';

/** Next 7 days against each ruta's cupo; a full day stands out and opens Rutas on that day. */
export function WeeklyLoad({ panel }: { panel: Panel }) {
  const navigate = useNavigate();
  const rutas = panel.load.rutas;
  const days = useMemo(() => panel.load.days, [panel.load.days]);
  return (
    <section className="card">
      <div className="card__header">
        <h2>Carga de los próximos 7 días</h2>
        <span className="muted">Paradas contra el cupo de cada ruta, con las fijas.</span>
      </div>
      <div className="card__body panel-load">
        {days.map((d) => {
          const over = d.rutas.some((x) => x.occupied > x.cupo);
          return (
            <button
              key={d.date}
              type="button"
              className={`panel-load__day${over ? ' over' : ''}`}
              onClick={() => navigate(`/rutas?fecha=${d.date}`)}
              title="Planear este día en Rutas"
            >
              <span className="panel-load__label">{d.label}</span>
              {d.nonWorking ? (
                <span className="muted">Inhábil</span>
              ) : (
                d.rutas.map((x) => {
                  const ruta = rutas.find((r) => r.id === x.rutaId);
                  const full = x.occupied >= x.cupo;
                  return (
                    <span key={x.rutaId} className={`occupancy${full ? ' full' : ''}`}>
                      <span className="occupancy__label">
                        <span className="ruta-color" style={{ background: ruta?.color }} />
                        {ruta?.code} {x.occupied}/{x.cupo}
                      </span>
                      <span className="occupancy__bar">
                        <span
                          style={{ width: `${Math.min(100, (x.occupied / Math.max(x.cupo, 1)) * 100)}%`, background: full ? undefined : ruta?.color }}
                        />
                      </span>
                    </span>
                  );
                })
              )}
            </button>
          );
        })}
      </div>
    </section>
  );
}

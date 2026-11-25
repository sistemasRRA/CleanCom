import { Link } from 'react-router';
import { type Card, money, type Panel, PROGRESS } from '../types/panel';
import { useSession } from '../../../shared/session/Session';
import { clock, hm } from '../../../shared/utils/dates';
import { Assignments } from './Assignments';

export function RutaCard({ card: r, panel, date }: { card: Card; panel: Panel; date: string }) {
  const { can } = useSession();
  const canEdit = can('panel-del-dia', 'edit') || can('rutas', 'edit');
  const progress = PROGRESS[r.progress];
  const goalPct = Math.min(100, (r.recolecciones / Math.max(r.goal, 1)) * 100);
  return (
    <article className={`card panel-ruta${r.alerts.length > 0 ? ' alert-left' : ''}`}>
      <div className="card__header">
        <div className="ruta-title">
          <span className="ruta-color big" style={{ background: r.color }} />
          <div>
            <h2>Ruta {r.code}</h2>
            <span className="muted">{r.zonas.length > 0 ? r.zonas.join(', ') : r.name}</span>
          </div>
        </div>
        <span className={`badge ${progress.tone}`}>{progress.label}</span>
      </div>
      <div className="card__body stack">
        <div>
          <div className="panel-goal">
            <span>
              Recolecciones <strong>{r.recolecciones}</strong> de {r.recoleccionesPlanned} · meta {r.goal}
            </span>
            <span>
              Entregas <strong>{r.entregas}</strong> de {r.entregasPlanned}
            </span>
          </div>
          <span className="occupancy__bar" aria-hidden>
            <span style={{ width: `${goalPct}%`, background: r.color }} />
          </span>
        </div>
        <div className="ruta-stats">
          <span>
            {r.closed}/{r.paradas} paradas
          </span>
          <span>{r.km.toFixed(1)} km</span>
          <span className={r.returnAt > r.maxReturn ? 'warn' : ''}>regreso {hm(r.returnAt)}</span>
          <span>cobrado {money(r.collected)}</span>
          <span>{r.lastLocationAt ? `GPS ${clock(r.lastLocationAt)}` : 'sin GPS'}</span>
        </div>
        <Assignments card={r} panel={panel} date={date} canEdit={canEdit} />
        {r.alerts.length > 0 && (
          <ul className="panel-alerts">
            {r.alerts.map((a) => (
              <li key={a}>{a}</li>
            ))}
          </ul>
        )}
        <Link to={`/rutas?fecha=${date}`} className="panel-link">
          Ver ruta
        </Link>
      </div>
    </article>
  );
}

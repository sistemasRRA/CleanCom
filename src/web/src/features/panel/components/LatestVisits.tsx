import { Link } from 'react-router';
import { KIND, type Panel, STATE } from '../types/panel';
import { clock } from '../../../shared/utils/dates';

export function LatestVisits({ panel }: { panel: Panel }) {
  return (
    <section className="card">
      <div className="card__header">
        <h2>Últimas visitas</h2>
        {panel.latestVisits.length > 0 && <Link to="/pedidos">Ver evidencias</Link>}
      </div>
      <div className="card__body">
        {panel.latestVisits.length === 0 ? (
          <div className="empty">Todavía no hay visitas cerradas este día.</div>
        ) : (
          <ul className="plain-list panel-visits">
            {panel.latestVisits.map((v) => (
              <li key={v.paradaId}>
                <span>
                  <strong>{v.name ?? 'Parada'}</strong> · Ruta {v.rutaCode} · {KIND[v.kind] ?? v.kind}
                  <span className={`badge ${v.state === 'Realizada' ? 'success' : 'danger'}`}>{STATE[v.state]}</span>
                </span>
                <span className="muted">
                  {clock(v.closedAt)} · {v.photos} foto{v.photos === 1 ? '' : 's'}
                  {v.signed ? ' · firma' : ''}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}

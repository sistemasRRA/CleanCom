import { useState } from 'react';
import type { Schemas } from '../../../shared/api/client';
import { HISTORY_FIRST, HISTORY_TONE, historyDay, historyTime } from '../types/pedidos';

/** History as a timeline grouped by day (newest first), the latest movements first and the rest on demand. */
export function Historial({ history }: { history: Schemas['PedidoEventoDto'][] }) {
  const [all, setAll] = useState(false);
  const shown = all ? history : history.slice(0, HISTORY_FIRST);
  const days: { day: string; items: Schemas['PedidoEventoDto'][] }[] = [];
  for (const h of shown) {
    const day = historyDay.format(new Date(h.at));
    const last = days[days.length - 1];
    if (last?.day === day) last.items.push(h);
    else days.push({ day, items: [h] });
  }
  return (
    <>
      <h3 className="form-section">Historial</h3>
      {history.length === 0 && <p className="muted">Sin movimientos.</p>}
      <div className="history">
        {days.map(({ day, items }) => (
          <section key={day} className="history__day">
            <h4 className="history__date">{day.charAt(0).toUpperCase() + day.slice(1)}</h4>
            <ol>
              {items.map((h) => (
                <li key={h.id} className={`history__item ${HISTORY_TONE[h.kind]}`}>
                  <span className="history__dot" aria-hidden="true" />
                  <div className="history__content">
                    {h.kind === 'Comentario' && (h.note || h.reason) ? (
                      <>
                        <span>Comentario</span>
                        <blockquote className="history__comment">{h.note ?? h.reason}</blockquote>
                      </>
                    ) : (
                      <span>{h.text}</span>
                    )}
                    <span className="history__meta">
                      {historyTime.format(new Date(h.at))}
                      {h.by && ` · ${h.by}`}
                    </span>
                  </div>
                </li>
              ))}
            </ol>
          </section>
        ))}
      </div>
      {history.length > HISTORY_FIRST && (
        <button type="button" className="link history__toggle" onClick={() => setAll((x) => !x)} aria-expanded={all}>
          {all ? 'Ver menos' : `Ver todo (${history.length} movimientos)`}
        </button>
      )}
    </>
  );
}

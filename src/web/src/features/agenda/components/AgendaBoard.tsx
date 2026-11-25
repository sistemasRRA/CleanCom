import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { addDays, mondayOf, monthTitle, type Pedido, toDate, type View } from '../types/board';
import { formatDate, todayInMexicoCity } from '../../../shared/utils/dates';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { QueryState } from '../../../shared/components/Page';
import { WeekView } from './WeekView';
import { DayView } from './DayView';
import { PedidoDetail } from './AgendaPedidoDetail';
import { agendaApi } from '../services/agendaApi';

/**
 * Agenda of recolecciones: week (7 days with the occupancy of each ruta) or day (one column per ruta, rows by turno).
 * Occupancy and cupo come computed from the API (GET /agenda/tablero); the screen only draws them.
 */
export function AgendaBoard() {
  const [view, setView] = useState<View>('semana');
  const [date, setDate] = useState(todayInMexicoCity);
  const [detail, setDetail] = useState<Pedido | null>(null);

  const from = view === 'semana' ? mondayOf(date) : date;
  const days = view === 'semana' ? 7 : 1;
  const board = useQuery({
    queryKey: [...keys.agendaBoard, from, days],
    queryFn: () => agendaApi.board({ from, days }),
    placeholderData: (previous) => previous,
  });
  const b = board.data;
  const today = b?.today ?? todayInMexicoCity();

  const step = view === 'semana' ? 7 : 1;
  const title = view === 'semana' ? monthTitle.format(toDate(from)) : formatDate(date);

  return (
    <section className="card">
      <div className="card__header agenda__toolbar">
        <div className="actions">
          <h2>Agenda</h2>
          <button type="button" className="small" onClick={() => setDate(addDays(date, -step))} aria-label="Anterior">
            ‹
          </button>
          <button type="button" className="small" onClick={() => setDate(today)}>
            Hoy
          </button>
          <button type="button" className="small" onClick={() => setDate(addDays(date, step))} aria-label="Siguiente">
            ›
          </button>
          <strong className="agenda__title">{title}</strong>
        </div>
        <div className="actions">
          <input type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} aria-label="Ir a la fecha" />
          <div className="segmented">
            {(['semana', 'dia'] as View[]).map((v) => (
              <button key={v} type="button" className={`segmented__option${view === v ? ' selected' : ''}`} onClick={() => setView(v)}>
                {v === 'semana' ? 'Semana' : 'Día'}
              </button>
            ))}
          </div>
        </div>
      </div>
      <QueryState loading={board.isPending} error={board.error}>
        {b &&
          (view === 'semana' ? (
            <WeekView
              board={b}
              today={today}
              selected={date}
              onDay={(d) => {
                setDate(d);
                setView('dia');
              }}
              onPedido={setDetail}
            />
          ) : (
            <DayView board={b} onPedido={setDetail} />
          ))}
      </QueryState>
      {detail && <PedidoDetail pedido={detail} ruta={b?.rutas.find((r) => r.code === detail.rutaCode)} onClose={() => setDetail(null)} />}
    </section>
  );
}

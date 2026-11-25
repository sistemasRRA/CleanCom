import { type Board, type Pedido, sortPedidos, toDate, weekday } from '../types/board';
import { Occupancy } from './Occupancy';
import { PedidoCard } from './AgendaPedidoCard';

export function WeekView({
  board,
  today,
  selected,
  onDay,
  onPedido,
}: {
  board: Board;
  today: string;
  selected: string;
  onDay: (date: string) => void;
  onPedido: (p: Pedido) => void;
}) {
  return (
    <div className="agenda-week">
      {board.days.map((d) => {
        const list = sortPedidos(board.pedidos.filter((p) => p.recoleccionDate === d.date));
        return (
          <div
            key={d.date}
            className={`agenda-day${d.date === today ? ' today' : ''}${d.date === selected ? ' selected' : ''}${d.nonWorking ? ' off' : ''}`}
          >
            <button type="button" className="agenda-day__header" onClick={() => onDay(d.date)} title="Ver el día">
              <span className="muted">{weekday.format(toDate(d.date))}</span>
              <span className="agenda-day__number">{toDate(d.date).getUTCDate()}</span>
            </button>
            {d.nonWorking ? (
              <div className="badge warning">Inhábil: {d.nonWorking}</div>
            ) : (
              <div className="agenda-day__occupancy">
                {board.rutas.map((r) => {
                  const o = d.rutas.find((x) => x.rutaId === r.id);
                  return o && (o.occupied > 0 || d.date >= today) ? <Occupancy key={r.id} ruta={r} occupied={o.occupied} cupo={o.cupo} /> : null;
                })}
              </div>
            )}
            <div className="agenda-day__list">
              {list.map((p) => (
                <PedidoCard key={p.id} pedido={p} ruta={board.rutas.find((r) => r.code === p.rutaCode)} onClick={() => onPedido(p)} compact />
              ))}
              {list.length === 0 && !d.nonWorking && <span className="muted agenda-day__empty">Sin recolecciones</span>}
            </div>
          </div>
        );
      })}
    </div>
  );
}

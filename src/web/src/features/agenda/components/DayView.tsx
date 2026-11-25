import { type Board, type Pedido, sortPedidos, TURNOS } from '../types/board';
import { Occupancy } from './Occupancy';
import { PedidoCard } from './AgendaPedidoCard';

export function DayView({ board, onPedido }: { board: Board; onPedido: (p: Pedido) => void }) {
  const day = board.days[0];
  if (!day) return null;
  if (board.rutas.length === 0) return <div className="empty">No hay rutas activas.</div>;
  return (
    <div className="agenda-dayview" style={{ gridTemplateColumns: `140px repeat(${board.rutas.length}, minmax(220px, 1fr))` }}>
      <div />
      {board.rutas.map((r) => {
        const o = day.rutas.find((x) => x.rutaId === r.id);
        return (
          <div key={r.id} className="agenda-dayview__ruta">
            <strong>{r.name}</strong>
            {o && <Occupancy ruta={r} occupied={o.occupied} cupo={o.cupo} />}
          </div>
        );
      })}
      {day.nonWorking && <div className="alert agenda-dayview__off">Día inhábil: {day.nonWorking}</div>}
      {TURNOS.map((t) => (
        <DayRow key={t.key} turno={t} board={board} onPedido={onPedido} />
      ))}
    </div>
  );
}

function DayRow({ turno, board, onPedido }: { turno: (typeof TURNOS)[number]; board: Board; onPedido: (p: Pedido) => void }) {
  return (
    <>
      <div className="agenda-dayview__turno">
        <strong>{turno.name}</strong>
        <span className="muted">{turno.hint}</span>
      </div>
      {board.rutas.map((r) => {
        const list = sortPedidos(board.pedidos.filter((p) => p.rutaCode === r.code && p.turno === turno.key));
        return (
          <div key={r.id} className="agenda-dayview__cell">
            {list.map((p) => (
              <PedidoCard key={p.id} pedido={p} ruta={r} onClick={() => onPedido(p)} />
            ))}
          </div>
        );
      })}
    </>
  );
}

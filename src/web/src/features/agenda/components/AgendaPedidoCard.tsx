import { type Pedido, type Ruta, turnoShort } from '../types/board';

export function PedidoCard({ pedido: p, ruta, onClick, compact }: { pedido: Pedido; ruta?: Ruta; onClick: () => void; compact?: boolean }) {
  const when = p.turno === 'HoraEspecifica' && p.specificTime ? p.specificTime.slice(0, 5) : turnoShort[p.turno];
  return (
    <button type="button" className="agenda-card" style={{ borderLeftColor: ruta?.color }} onClick={onClick}>
      <span className="agenda-card__top">
        <strong>{p.invitadoName}</strong>
        {p.priority && <span className="badge danger">Prioridad</span>}
      </span>
      <span className="muted">
        {p.zonaCode ?? 'Sin zona'} · {when}
      </span>
      {!compact && (
        <span className="muted">
          {p.lineasServicio.join(', ') || 'Sin línea'}
          {p.garmentsApprox != null && ` · ${p.garmentsApprox} prendas`}
        </span>
      )}
    </button>
  );
}

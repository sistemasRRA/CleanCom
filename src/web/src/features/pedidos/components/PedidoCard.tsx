import type { Card } from '../types/pedidos';
import { shortDay } from '../../../shared/utils/dates';

export function PedidoCard({ card: c, onOpen }: { card: Card; onOpen: () => void }) {
  const entrega = ['EntregaConfirmada', 'EnCamino', 'Entregado'].includes(c.etapa);
  return (
    <button type="button" className={`pedido-card ${c.delayLevel === 'Late' ? 'late' : c.delayLevel === 'Warning' ? 'warn' : ''}`} onClick={onOpen}>
      <div className="pedido-card__top">
        {c.rutaCode && (
          <span>
            <span className="ruta-color" style={{ background: c.rutaColor ?? undefined }} />
            {c.rutaCode}
          </span>
        )}
        <span className="muted">#{c.folio}</span>
        {c.priority && <span className="badge danger">Prioridad</span>}
      </div>
      <strong>{c.invitadoName}</strong>
      <span className="muted">
        {entrega ? `Entrega ${shortDay(c.entregaDate)}` : `Recolección ${shortDay(c.recoleccionDate)}`}
        {c.garmentsApprox != null && ` · ${c.garmentsApprox} prendas`}
      </span>
      {c.daysInStore != null && <span className="muted">{c.daysInStore} días en tienda</span>}
      {c.ticketsTotal > 0 && (
        <span className={`badge ${c.ticketsReady === c.ticketsTotal ? 'success' : ''}`}>
          {c.ticketsReady}/{c.ticketsTotal} tickets listos
        </span>
      )}
      {c.delayText && <span className={`badge ${c.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{c.delayText}</span>}
      {c.failedAttempts > 0 && c.etapa !== 'Entregado' && (
        <span className="badge danger">
          {c.failedAttempts} intento{c.failedAttempts > 1 ? 's' : ''} fallido{c.failedAttempts > 1 ? 's' : ''}
        </span>
      )}
    </button>
  );
}

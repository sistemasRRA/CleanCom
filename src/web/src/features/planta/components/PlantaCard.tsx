import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { Card, Ticket } from '../types/planta';
import '../../../shared/api/client';
import { refetch } from '../services/queryKeys';
import { shortDay } from '../../../shared/utils/dates';
import { plantaApi } from '../services/plantaApi';

export function PlantaCard({
  card: c,
  canEdit,
  onComment,
  onBaja,
}: {
  card: Card;
  canEdit: boolean;
  onComment: () => void;
  onBaja: (t: Ticket) => void;
}) {
  const queryClient = useQueryClient();
  const ready = useMutation({
    mutationFn: (folio: string) => plantaApi.markReady(folio),
    onSuccess: () => Promise.all(refetch.map((k) => queryClient.invalidateQueries({ queryKey: k }))),
  });
  const p = c.pedido;
  const late = c.tickets.some((t) => t.delayLevel === 'Late');

  return (
    <article className={`planta-card${c.entregaDueOrLate || late ? ' late' : ''}`}>
      <header>
        {p.rutaCode && (
          <span>
            <span className="ruta-color" style={{ background: p.rutaColor ?? undefined }} />
            {p.rutaCode}
          </span>
        )}
        <span className="muted">#{p.folio}</span>
        <span className={`badge ${c.ticketsReady === c.ticketsTotal ? 'success' : ''}`}>
          {c.ticketsReady}/{c.ticketsTotal} listos
        </span>
      </header>
      <strong>{p.invitadoName}</strong>
      <span className="muted">
        {c.daysInPlanta} día{c.daysInPlanta === 1 ? '' : 's'} en planta
        {p.entregaDate && ` · entrega ${shortDay(p.entregaDate)}`}
      </span>
      {c.entregaDueOrLate && <span className="badge danger">Entrega hoy o atrasada</span>}
      <ul className="planta-tickets">
        {c.tickets.map((t) => (
          <li key={t.id} className={t.state === 'Listo' ? 'done' : ''}>
            <div>
              <strong>{t.folio}</strong>
              <span className="muted">
                {t.lineaName} · {t.garments} prenda{t.garments === 1 ? '' : 's'}
              </span>
              {t.delayText && <span className={`badge ${t.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{t.delayText}</span>}
            </div>
            {t.state === 'Listo' ? (
              <span className="badge success">Listo</span>
            ) : (
              canEdit && (
                <span className="actions tight">
                  <button type="button" className="small" title="Dar de baja (capturado por error)" onClick={() => onBaja(t)}>
                    Baja
                  </button>
                  <button type="button" className="primary" disabled={ready.isPending} onClick={() => ready.mutate(t.folio)}>
                    Listo
                  </button>
                </span>
              )
            )}
          </li>
        ))}
      </ul>
      {ready.error && <div className="alert error">{ready.error.message}</div>}
      {c.lastDelayNote && <p className="muted">Atraso: {c.lastDelayNote}</p>}
      {canEdit && c.ticketsReady < c.ticketsTotal && (
        <button type="button" className="small link" onClick={onComment}>
          ¿Por qué se atrasa? Comentar
        </button>
      )}
    </article>
  );
}

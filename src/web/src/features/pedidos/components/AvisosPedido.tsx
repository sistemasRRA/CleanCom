import { type Schemas } from '../../../shared/api/client';
import { useSession } from '../../../shared/session/Session';
import { useSave } from '../../../shared/hooks/useSave';
import { AVISO_STATE, avisosKey, CANAL, SendByHand, avisosApi } from '../../avisos';
import { keys } from '../../../shared/api/queryKeys';
import { formatDateTime } from '../../../shared/utils/dates';

/** Avisos sent to the invitado about this pedido, with "Abrir WhatsApp" for the ones to send by hand. */
export function AvisosPedido({ avisos, pedidoId }: { avisos: Schemas['AvisoDto'][]; pedidoId: number }) {
  const { can } = useSession();
  const canSend = can('whatsapp-y-metas', 'edit') || can('pedidos', 'edit');
  const sent = useSave((id: number) => avisosApi.markSentByHand(id), {
    refetch: [avisosKey, [...keys.pedidos, 'detalle', pedidoId]],
    message: 'Aviso marcado como mandado.',
  });
  return (
    <>
      <h3 className="form-section">Avisos al invitado</h3>
      <ul className="plain-list">
        {avisos.map((a) => (
          <li key={a.id} style={{ display: 'grid', gap: 2 }}>
            <span>
              <strong>{a.eventoName}</strong> · {a.canal ? CANAL[a.canal] : 'sin canal'} ·{' '}
              <span className={`badge ${AVISO_STATE[a.state].tone}`}>{AVISO_STATE[a.state].text}</span>{' '}
              <span className="muted">{formatDateTime(a.createdAt)}</span>
            </span>
            <span className="muted">
              {a.title && `${a.title} · `}
              {a.text}
              {a.detail && ` · ${a.detail}`}
            </span>
            <SendByHand aviso={a} canSend={canSend} busy={sent.isPending} onSent={(id) => sent.mutate(id)} />
          </li>
        ))}
      </ul>
      {sent.error && <div className="alert error">{sent.error.message}</div>}
    </>
  );
}

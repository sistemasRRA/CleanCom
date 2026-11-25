import { Link } from 'react-router';
import { type Aviso, AVISO_STATE, CANAL } from '../types/avisos';
import { useSession } from '../../../shared/session/Session';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { avisosKey } from '../services/queryKeys';
import { formatDateTime } from '../../../shared/utils/dates';
import { SendByHand } from './SendByHand';
import { avisosApi } from '../services/avisosApi';

export function AvisosTable({ avisos, toSend }: { avisos: Aviso[]; toSend: boolean }) {
  const { can } = useSession();
  const canSend = can('whatsapp-y-metas', 'edit') || can('pedidos', 'edit');
  const sent = useSave((id: number) => avisosApi.markSentByHand(id), {
    refetch: [avisosKey],
    message: 'Aviso marcado como mandado.',
  });
  return (
    <section className="card">
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Invitado</th>
              <th>Aviso</th>
              <th>Texto</th>
              <th>Canal</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {avisos.length === 0 && (
              <tr>
                <td colSpan={7} className="muted">
                  {toSend ? 'No hay avisos por mandar.' : 'Todavía no hay avisos.'}
                </td>
              </tr>
            )}
            {avisos.map((a) => (
              <tr key={a.id}>
                <td>{formatDateTime(a.createdAt)}</td>
                <td>
                  <Link to={`/clientes?invitado=${a.invitadoId}`}>{a.invitadoName}</Link>
                  {a.phone && <div className="muted">{a.phone}</div>}
                </td>
                <td>
                  {a.eventoName}
                  {a.pedidoId && (
                    <div>
                      <Link to={`/pedidos?pedido=${a.pedidoId}`}>Pedido {a.pedidoId}</Link>
                    </div>
                  )}
                </td>
                <td style={{ maxWidth: 380 }}>
                  {a.title && <strong>{a.title} · </strong>}
                  {a.text}
                  {a.detail && <div className="muted">{a.detail}</div>}
                </td>
                <td>{a.canal ? CANAL[a.canal] : '—'}</td>
                <td>
                  <span className={`badge ${AVISO_STATE[a.state].tone}`}>{AVISO_STATE[a.state].text}</span>
                  {a.sentAt && <div className="muted">{formatDateTime(a.sentAt)}</div>}
                </td>
                <td className="end">
                  <SendByHand aviso={a} canSend={canSend} busy={sent.isPending} onSent={(id) => sent.mutate(id)} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {sent.error && <div className="alert error">{sent.error.message}</div>}
    </section>
  );
}

import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router';
import { useSession } from '../../../shared/session/Session';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { Dialog } from '../../../shared/components/Dialog';
import { QueryState } from '../../../shared/components/Page';
import { etapaName } from '../types/pedidos';
import { formatDateTime, shortDay } from '../../../shared/utils/dates';
import { MoveForm } from './MoveForm';
import { EntregaForm, entregaTurno } from './EntregaForm';
import { NoEntregadoForm } from './NoEntregadoForm';
import { CommentForm } from './CommentForm';
import { BeneficioLine } from '../../promociones';
import { AvisosPedido } from './AvisosPedido';
import { Visitas } from './Visitas';
import { Historial } from './Historial';
import { pedidosApi } from '../services/pedidosApi';

type Action = 'mover' | 'entrega' | 'no-entregado' | 'comentar' | null;

export function PedidoDialog({ id, onClose }: { id: number; onClose: () => void }) {
  const canEdit = useSession().can('pedidos', 'edit');
  const detail = useQuery({ queryKey: [...keys.pedidos, 'detalle', id], queryFn: () => pedidosApi.get(id) });
  const [action, setAction] = useState<Action>(null);
  const d = detail.data;

  return (
    <Dialog
      title={d ? `Pedido #${d.card.folio} · ${d.card.invitadoName}` : 'Pedido'}
      onClose={onClose}
      onSubmit={onClose}
      submitText="Cerrar"
      hideCancel
      className="pedido-dialog"
    >
      <QueryState loading={detail.isPending} error={detail.error}>
        {d && (
          <div className="stack">
            <div className="pedido-summary">
              <span className="badge brand">{etapaName(d.card.etapa)}</span>
              {d.card.delayText && <span className={`badge ${d.card.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{d.card.delayText}</span>}
              {d.card.failedAttempts > 0 && <span className="badge danger">{d.card.failedAttempts} intento(s) fallido(s)</span>}
            </div>
            <dl className="pedido-facts">
              <dt>Dirección</dt>
              <dd>
                {d.address}
                {d.card.zonaCode && ` · ${d.card.zonaCode}`}
                {d.card.rutaCode && ` · Ruta ${d.card.rutaCode}`}
              </dd>
              <dt>Recolección</dt>
              <dd>{shortDay(d.card.recoleccionDate)}</dd>
              <dt>Entrega</dt>
              <dd>{shortDay(d.card.entregaDate)}</dd>
              <dt>Prendas · líneas</dt>
              <dd>
                {d.card.garmentsApprox ?? '—'} · {d.card.lineas.join(', ') || '—'}
              </dd>
              {d.driverNotes && (
                <>
                  <dt>Notas</dt>
                  <dd>{d.driverNotes}</dd>
                </>
              )}
              <dt>Invitado</dt>
              <dd>
                {d.card.phone}{' '}
                <Link to={`/clientes?invitado=${d.card.invitadoId}`} onClick={onClose}>
                  Ver en CRM
                </Link>
              </dd>
            </dl>

            {canEdit && d.card.etapa !== 'Cancelado' && (
              <div className="actions tight pedido-actions">
                <button type="button" className={`small${action === 'mover' ? ' selected' : ''}`} onClick={() => setAction('mover')}>
                  Cambiar etapa
                </button>
                {['EnPlanta', 'Listo', 'EntregaConfirmada'].includes(d.card.etapa) && (
                  <button type="button" className={`small${action === 'entrega' ? ' selected' : ''}`} onClick={() => setAction('entrega')}>
                    {d.card.etapa === 'EntregaConfirmada' ? 'Cambiar fecha de entrega' : 'Programar entrega'}
                  </button>
                )}
                {['EntregaConfirmada', 'EnCamino'].includes(d.card.etapa) && (
                  <button type="button" className={`small${action === 'no-entregado' ? ' selected' : ''}`} onClick={() => setAction('no-entregado')}>
                    No se entregó
                  </button>
                )}
                <button type="button" className={`small${action === 'comentar' ? ' selected' : ''}`} onClick={() => setAction('comentar')}>
                  Comentar
                </button>
              </div>
            )}
            {action === 'mover' && <MoveForm detail={d} onDone={() => setAction(null)} />}
            {action === 'entrega' && <EntregaForm id={id} turno={entregaTurno(d.paradas)} onDone={() => setAction(null)} />}
            {action === 'no-entregado' && <NoEntregadoForm id={id} onDone={() => setAction(null)} />}
            {action === 'comentar' && <CommentForm id={id} onDone={() => setAction(null)} />}

            {d.tickets.length > 0 && (
              <>
                <h3 className="form-section">Tickets</h3>
                <ul className="plain-list">
                  {d.tickets.map((t) => (
                    <li key={t.id}>
                      <strong>{t.folio}</strong> · {t.lineaName} · {t.garments} prenda{t.garments === 1 ? '' : 's'} ·{' '}
                      {t.state === 'Listo' ? `listo ${formatDateTime(t.readyAt)}` : 'en proceso'}
                      {t.delayText && <span className={`badge ${t.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{t.delayText}</span>}
                    </li>
                  ))}
                </ul>
              </>
            )}
            {d.beneficios.length > 0 && (
              <>
                <h3 className="form-section">Beneficios</h3>
                <ul className="plain-list">
                  {d.beneficios.map((b) => (
                    <BeneficioLine key={b.id} b={b} />
                  ))}
                </ul>
              </>
            )}
            {d.avisos.length > 0 && <AvisosPedido avisos={d.avisos} pedidoId={d.card.id} />}
            <Visitas paradas={d.paradas} />
            <Historial history={d.history} />
          </div>
        )}
      </QueryState>
    </Dialog>
  );
}

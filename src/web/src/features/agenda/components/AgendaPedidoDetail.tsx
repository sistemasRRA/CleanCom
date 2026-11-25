import { useEffect, useRef } from 'react';
import { type Pedido, type Ruta, TURNOS } from '../types/board';
import { useSession } from '../../../shared/session/Session';
import { useConfirm } from '../../../shared/components/Toasts';
import { useAction } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { formatDate } from '../../../shared/utils/dates';
import { pedidosApi } from '../../pedidos';

export function PedidoDetail({ pedido: p, ruta, onClose }: { pedido: Pedido; ruta?: Ruta; onClose: () => void }) {
  const role = useSession().user?.role;
  const isAdmin = role === 'SuperUser' || role === 'Administracion';
  const ask = useConfirm();
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);
  const remove = useAction(
    (id: number) => pedidosApi.remove(id),
    [keys.agendaBoard, keys.pedidos, ['agenda']],
    'Recolección eliminada; su lugar quedó libre.',
  );

  return (
    <dialog ref={ref} onClose={onClose}>
      <div className="dialog__header">
        <h2>{p.invitadoName}</h2>
        <p className="muted">
          {formatDate(p.recoleccionDate)} · Ruta {p.rutaCode ?? '—'}
          {ruta && <span className="ruta-color" style={{ background: ruta.color, marginLeft: 6 }} />} · Zona {p.zonaCode ?? '—'}
        </p>
      </div>
      <div className="dialog__body">
        <dl className="detail-list">
          <dt>WhatsApp</dt>
          <dd>{p.whatsAppPhone}</dd>
          <dt>Dirección</dt>
          <dd>{p.address}</dd>
          <dt>Cuándo</dt>
          <dd>
            {TURNOS.find((t) => t.key === p.turno)?.name}
            {p.specificTime && ` ${p.specificTime.slice(0, 5)}`}
            {p.priority && ' · Prioridad'}
          </dd>
          <dt>Servicio</dt>
          <dd>
            {p.lineasServicio.join(', ') || '—'}
            {p.garmentsApprox != null && ` · ${p.garmentsApprox} prendas`}
          </dd>
          {p.driverNotes && (
            <>
              <dt>Notas para el chofer</dt>
              <dd>{p.driverNotes}</dd>
            </>
          )}
          <dt>Etapa</dt>
          <dd>{p.etapa}</dd>
          <dt>Origen</dt>
          <dd>{p.origin === 'App' ? 'App del invitado' : 'Oficina'}</dd>
        </dl>
      </div>
      <div className="dialog__footer">
        {isAdmin && (
          <button
            type="button"
            className="danger"
            disabled={remove.isPending}
            onClick={async () => {
              if (
                !(await ask(`¿Eliminar la recolección de ${p.invitadoName} del ${formatDate(p.recoleccionDate)}?`, {
                  detail: 'Su lugar en la agenda queda libre.',
                  confirmText: 'Eliminar',
                  danger: true,
                }))
              )
                return;
              remove.mutate(p.id, { onSuccess: onClose });
            }}
          >
            Eliminar
          </button>
        )}
        <button type="button" className="primary" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </dialog>
  );
}

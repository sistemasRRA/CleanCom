import { useState } from 'react';
import { createPortal } from 'react-dom';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { api, unwrap, type Schemas } from '../api/client';
import { useSession } from '../session/Session';
import { Dialog, formatDateTime, useSave } from './Common';

export const avisosOficinaKey = ['avisos-oficina'] as const;

const KIND: Record<Schemas['AvisoOficinaKind'], string> = {
  NuevoPedido: 'Pedido nuevo',
  CambioDeDia: 'Cambio de día',
  Cancelacion: 'Cancelación',
};

function BellIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </svg>
  );
}

/**
 * The office bell (menu): what the invitado did in the app (new pedido, day change, cancellation), so the office
 * sees it without opening each pedido. Anyone marks one as attended for everyone. Reloads every minute.
 */
export function AvisosOficinaBell() {
  const { can } = useSession();
  const allowed = can('agendar') || can('pedidos') || can('clientes-crm');
  const [open, setOpen] = useState(false);
  const avisos = useQuery({
    queryKey: avisosOficinaKey,
    queryFn: () => unwrap(api.GET('/avisos-oficina')),
    enabled: allowed,
    refetchInterval: 60_000,
  });
  if (!allowed) return null;
  const pending = avisos.data?.pending ?? 0;
  const label = pending > 0 ? `Avisos de la app (${pending} sin atender)` : 'Avisos de la app';

  return (
    <>
      <button type="button" className="menu__theme menu__bell" onClick={() => setOpen(true)} aria-label={label} data-tooltip={label}>
        <BellIcon />
        <span className="menu__theme-label">Avisos de la app</span>
        {pending > 0 && <span className="menu__bell-count">{pending}</span>}
      </button>
      {/* Outside the menu, so the menu styles do not reach its buttons. */}
      {open && createPortal(<AvisosOficinaDialog data={avisos.data} onClose={() => setOpen(false)} />, document.body)}
    </>
  );
}

function AvisosOficinaDialog({ data, onClose }: { data?: Schemas['AvisosOficinaDto']; onClose: () => void }) {
  const attend = useSave((id: number) => unwrap(api.POST('/avisos-oficina/{id}/atendido', { params: { path: { id } } })), {
    refetch: [avisosOficinaKey],
  });
  const list = data?.avisos ?? [];
  return (
    <Dialog title="Avisos de la app del invitado" onClose={onClose} onSubmit={onClose} submitText="Cerrar" hideCancel error={attend.error}>
      {list.length === 0 && (
        <p className="muted">Sin avisos. Aquí aparece lo que los invitados hacen en la app: pedidos nuevos, cambios de día y cancelaciones.</p>
      )}
      <ul className="plain-list">
        {list.map((a) => (
          <li key={a.id} className={a.attendedAt ? 'muted' : ''} style={{ display: 'grid', gap: 2 }}>
            <span>
              <strong>{KIND[a.kind]}</strong> · {a.invitadoName} <span className="muted">· {formatDateTime(a.createdAt)}</span>
            </span>
            <span>{a.text}</span>
            <span className="actions tight">
              {a.pedidoId && (
                <Link to={`/pedidos?pedido=${a.pedidoId}`} onClick={onClose}>
                  Ver pedido {a.pedidoId}
                </Link>
              )}
              {a.attendedAt ? (
                <span className="muted">Atendido {formatDateTime(a.attendedAt)}</span>
              ) : (
                <button type="button" className="small" disabled={attend.isPending} onClick={() => attend.mutate(a.id)}>
                  Atendido
                </button>
              )}
            </span>
          </li>
        ))}
      </ul>
    </Dialog>
  );
}

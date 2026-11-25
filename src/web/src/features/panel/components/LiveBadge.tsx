import type { LiveStatus } from '../../../shared/api/realtime';

export function LiveBadge({ status }: { status: LiveStatus }) {
  const text: Record<LiveStatus, string> = {
    connecting: 'Conectando…',
    live: 'En vivo',
    reconnecting: 'Reconectando…',
    offline: 'Sin conexión en vivo',
  };
  return (
    <span className={`badge ${status === 'live' ? 'success' : status === 'offline' ? 'danger' : ''}`} title="Actualización en tiempo real">
      {text[status]}
    </span>
  );
}

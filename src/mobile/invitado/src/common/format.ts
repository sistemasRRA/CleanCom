import type { Schemas } from '@cleancom/mobile-shared';
import Ionicons from '@expo/vector-icons/Ionicons';

/** "2026-11-23" → "lun 23 nov". Business dates are already Mexico City days. */
export function day(iso: string | null | undefined, long = false) {
  if (!iso) return '';
  const d = new Date(`${iso.slice(0, 10)}T12:00:00Z`);
  return d.toLocaleDateString('es-MX', { weekday: long ? 'long' : 'short', day: 'numeric', month: long ? 'long' : 'short', timeZone: 'UTC' });
}

/** A time of the API (UTC) in Mexico City, "10:42". */
export function clock(iso: string | null | undefined) {
  return iso ? new Date(iso).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' }) : '';
}

export const TURNOS: { key: Schemas['Turno']; label: string; note: string }[] = [
  { key: 'CualquierHora', label: 'Cualquier hora', note: '9:00 a 18:00' },
  { key: 'Manana', label: 'Mañana', note: '9:00 a 13:00' },
  { key: 'Tarde', label: 'Tarde', note: '13:00 a 18:00' },
];

export const ETAPA_ICON: Record<Schemas['PedidoEtapa'], keyof typeof Ionicons.glyphMap> = {
  Agendado: 'calendar-outline',
  Recolectado: 'bag-handle-outline',
  EnPlanta: 'water-outline',
  Listo: 'checkmark-done-outline',
  EntregaConfirmada: 'calendar-number-outline',
  EnCamino: 'car-outline',
  Entregado: 'home-outline',
  Cancelado: 'close-circle-outline',
};

/** The address in one line. */
export function addressLine(a: Schemas['AddressDto']) {
  const street = [a.street, a.exteriorNumber, a.interiorNumber ? `int. ${a.interiorNumber}` : null].filter(Boolean).join(' ');
  return [street, a.neighborhood].filter(Boolean).join(', ');
}

/** A moment of the API in Mexico City, "lun 23 nov, 10:42". */
export function stamp(iso: string | null | undefined) {
  return iso
    ? new Date(iso).toLocaleString('es-MX', {
        weekday: 'short',
        day: 'numeric',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
        timeZone: 'America/Mexico_City',
      })
    : '';
}

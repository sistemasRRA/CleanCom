import type { Schemas } from '@cleancom/mobile-shared';

export type Ruta = Schemas['ChoferRutaDto'];

export type Shift = Schemas['ChoferShiftDto'];

export type Unidad = Schemas['ChoferUnidadDto'];

export type Severity = Schemas['FailureSeverity'];

export const SEVERITIES: { key: Severity; label: string; hint: string }[] = [
  { key: 'Baja', label: 'Baja', hint: 'Puedo seguir la ruta' },
  { key: 'Media', label: 'Media', hint: 'Hay que revisarla pronto' },
  { key: 'Alta', label: 'Alta', hint: 'No es seguro seguir' },
];

/** "8:05 a. m." in Mexico City time. */
export const time = (iso: string) =>
  new Date(iso).toLocaleTimeString('es-MX', { hour: 'numeric', minute: '2-digit', timeZone: 'America/Mexico_City' });

/** A whole number typed by the chofer ("45 210" or "45,210" → 45210), or null when empty or not a number. */
export const parseKm = (text: string): number | null => {
  const digits = text.replace(/\D/g, '');
  return digits ? Number(digits) : null;
};

/** An amount typed by the chofer ("350.50" or "350,50"), or null when empty or not a number. */
export const parseAmount = (text: string): number | null => {
  const clean = text.trim().replace(/\$|\s/g, '').replace(',', '.');
  if (!clean) return null;
  const n = Number(clean);
  return Number.isFinite(n) && n >= 0 ? n : null;
};

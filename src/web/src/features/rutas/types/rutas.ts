import type { Schemas } from '../../../shared/api/client';

export type Plan = Schemas['RutaPlanDto'];

export type Parada = Schemas['RutaPlanParadaDto'];

export type Kind = Schemas['ParadaKind'];

export const KIND: Record<Kind, string> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Entrega y recolección',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
  Salida: 'Salida',
  Regreso: 'Regreso',
  Comida: 'Comida',
  Traslado: 'Traslado',
};

export const CATEGORY: Record<NonNullable<Schemas['ParadaCategory']>, string> = {
  ClienteFinal: 'Cliente final',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
};

export const hm = (t?: string | null) => (t ? t.slice(0, 5) : '—');

const clockFormat = new Intl.DateTimeFormat('es-MX', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'America/Mexico_City' });

/** UTC instant from the API in Mexico City time. */
export const clock = (iso?: string | null) => (iso ? clockFormat.format(new Date(iso)) : '');

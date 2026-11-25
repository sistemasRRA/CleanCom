import type { Schemas } from '@cleancom/mobile-shared';

export type Ruta = Schemas['ChoferRutaDto'];

export type Parada = Schemas['ChoferParadaDto'];

export const KIND: Record<Schemas['ParadaKind'], string> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Recolección y entrega',
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

export const TURNO: Record<Schemas['Turno'], string> = {
  Manana: 'Mañana',
  Tarde: 'Tarde',
  CualquierHora: 'Cualquier hora',
  HoraEspecifica: 'Hora específica',
};

/** yyyy-MM-dd plus n days, without time zone shifts. */
export function addDays(iso: string, n: number) {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d + n)).toISOString().slice(0, 10);
}

const clock = new Intl.DateTimeFormat('es-MX', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'America/Mexico_City' });

export const at = (iso?: string | null) => (iso ? clock.format(new Date(iso)) : '');

export const pointOf = (p: Parada) => (p.lat != null && p.lng != null ? { lat: p.lat, lng: p.lng } : null);

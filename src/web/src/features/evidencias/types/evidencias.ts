import type { Schemas } from '../../../shared/api/client';

export type Visita = Schemas['VisitaDto'];
export type Visitas = Schemas['VisitasDto'];
export type Corte = Schemas['CorteDto'];
export type CorteLine = Schemas['CorteLineDto'];
export type Method = Schemas['PaymentMethod'];
export type CorteState = Schemas['CorteState'];

export const METHODS: { key: Method; name: string }[] = [
  { key: 'Efectivo', name: 'Efectivo' },
  { key: 'Tarjeta', name: 'Tarjeta' },
  { key: 'Transferencia', name: 'Transferencia' },
];

export const METHOD_NAME: Record<Method, string> = { Efectivo: 'Efectivo', Tarjeta: 'Tarjeta', Transferencia: 'Transferencia' };

export const KIND: Record<Schemas['ParadaKind'], string> = {
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

export const CORTE_STATE: Record<CorteState, { name: string; tone: string }> = {
  SinTurno: { name: 'Sin turno', tone: '' },
  TurnoAbierto: { name: 'Turno abierto', tone: 'brand' },
  PorRevisar: { name: 'Por revisar', tone: 'warning' },
  Cerrado: { name: 'Cerrado', tone: 'success' },
};

export const money = (n?: number | null) =>
  n == null ? '—' : n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 2 });

/** "+$20.00" / "−$20.00" / "$0.00": a difference, with its sign. */
export const difference = (n?: number | null) => (n == null ? '—' : n > 0 ? `+${money(n)}` : n < 0 ? `−${money(-n)}` : money(0));

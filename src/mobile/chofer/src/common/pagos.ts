import type { Schemas } from '@cleancom/mobile-shared';

/** Formas de pago of the reference payments (cerrar visita and the corte de caja at the end of the shift). */
export type Method = Schemas['PaymentMethod'];

export const METHODS: { key: Method; label: string }[] = [
  { key: 'Efectivo', label: 'Efectivo' },
  { key: 'Tarjeta', label: 'Tarjeta' },
  { key: 'Transferencia', label: 'Transferencia' },
];

export const money = (n: number) => n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 2 });

export const km = (n: number) => `${n.toLocaleString('es-MX')} km`;

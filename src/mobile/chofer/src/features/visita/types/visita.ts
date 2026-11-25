import type { Schemas } from '@cleancom/mobile-shared';

export type Parada = Schemas['ChoferParadaDto'];

export type Ruta = Schemas['ChoferRutaDto'];

export type Method = Schemas['PaymentMethod'];

export const METHODS: { key: Method; label: string }[] = [
  { key: 'Efectivo', label: 'Efectivo' },
  { key: 'Tarjeta', label: 'Tarjeta' },
  { key: 'Transferencia', label: 'Transferencia' },
];

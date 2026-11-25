import type { Schemas } from '../../../shared/api/client';

export type Promocion = Schemas['PromocionDto'];

export type Programa = Schemas['PromocionPrograma'];

export type DiscountType = Schemas['DiscountType'];

export type PaymentMethod = NonNullable<Schemas['PaymentMethod']>;

export const PROGRAMA: Record<Programa, string> = {
  Sellos: 'Sellos',
  Cuponera: 'Cuponera de bienvenida',
  Campana: 'Campañas',
};

export const DISCOUNT: Record<DiscountType, string> = {
  Porcentaje: 'Porcentaje',
  Monto: 'Monto en pesos',
  ArticuloGratis: 'Artículo gratis',
  PrecioEspecial: 'Precio especial',
};

export const PAYMENT: Record<PaymentMethod, string> = {
  Efectivo: 'Efectivo',
  Tarjeta: 'Tarjeta',
  Transferencia: 'Transferencia',
};

export const validityText = (days?: number | null) => (days == null ? 'Hasta la entrega del pedido' : `${days} días`);

export const percent = (n?: number | null) => (n == null ? '—' : `${n.toLocaleString('es-MX', { maximumFractionDigits: 1 })}%`);

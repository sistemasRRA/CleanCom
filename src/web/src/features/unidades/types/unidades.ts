import type { Schemas } from '../../../shared/api/client';

export type Card = Schemas['UnidadCardDto'];

export type Detail = Schemas['UnidadDetailDto'];

export type DocumentKind = Schemas['UnidadDocumentKind'];

export type EventKind = Schemas['UnidadEventKind'];

export type Severity = NonNullable<Schemas['FailureSeverity']>;

export type Frequency = Schemas['PaymentFrequency'];

type AlertEvent = Schemas['FleetAlertEvent'];

export const EVENT: Record<EventKind, string> = {
  Servicio: 'Servicio',
  Reparacion: 'Reparación',
  Falla: 'Falla',
  Pago: 'Pago',
  Kilometraje: 'Kilometraje',
};

export const DOCUMENT: Record<DocumentKind, string> = {
  Seguro: 'Póliza de seguro',
  TarjetaCirculacion: 'Tarjeta de circulación',
  Verificacion: 'Verificación',
  Comprobante: 'Comprobante',
  Otro: 'Otro',
  Foto: 'Foto de la unidad',
};

export const SEVERITY: Record<Severity, string> = { Baja: 'Baja', Media: 'Media', Alta: 'Alta' };

export const FREQUENCY: Record<Frequency, string> = {
  Mensual: 'Mensual',
  Bimestral: 'Bimestral',
  Trimestral: 'Trimestral',
  Semestral: 'Semestral',
  Anual: 'Anual',
};

export const ALERT_EVENT: Record<AlertEvent, string> = {
  Verificacion: 'Verificación',
  Seguro: 'Seguro',
  PagoRecurrente: 'Pagos recurrentes',
  TarjetaCirculacion: 'Tarjeta de circulación',
  Servicio: 'Servicio',
  Falla: 'Falla reportada',
};

export const LEVEL_TONE: Record<Schemas['FleetAlertLevel'], string> = { Vencido: 'danger', Falla: 'danger', PorVencer: 'warning', SinRegistro: '' };

export const money = (n?: number | null) =>
  n == null ? '—' : n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 2 });

export const km = (n?: number | null) => (n == null ? '—' : `${n.toLocaleString('es-MX')} km`);

export const day = (iso?: string | null) =>
  iso
    ? new Date(`${iso.slice(0, 10)}T12:00:00Z`).toLocaleDateString('es-MX', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })
    : '—';

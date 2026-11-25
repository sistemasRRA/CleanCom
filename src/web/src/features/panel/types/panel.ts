import type { Schemas } from '../../../shared/api/client';

export type Panel = Schemas['PanelDto'];

export type Card = Schemas['PanelRutaDto'];

type Progress = Schemas['RutaProgress'];

export const PROGRESS: Record<Progress, { label: string; tone: string }> = {
  SinSalir: { label: 'Sin salir', tone: '' },
  EnRuta: { label: 'En ruta', tone: 'brand' },
  DeRegreso: { label: 'De regreso', tone: 'success' },
};

export const KIND: Partial<Record<Schemas['ParadaKind'], string>> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Entrega y recolección',
};

export const STATE: Record<Schemas['ParadaState'], string> = {
  Pendiente: 'Pendiente',
  EnCamino: 'En camino',
  EnSitio: 'En sitio',
  Realizada: 'Realizada',
  NoRealizada: 'No realizada',
};

export const money = (n: number) => n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 });

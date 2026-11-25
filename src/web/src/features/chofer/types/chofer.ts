import type { Schemas } from '../../../shared/api/client';

export type Vista = Schemas['ChoferRutaDto'];

export type Parada = Schemas['ChoferParadaDto'];

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

export const STATE: Record<Schemas['ParadaState'], { label: string; tone: string }> = {
  Pendiente: { label: 'Pendiente', tone: '' },
  EnCamino: { label: 'En camino', tone: 'brand' },
  EnSitio: { label: 'En sitio', tone: 'warning' },
  Realizada: { label: 'Realizada', tone: 'success' },
  NoRealizada: { label: 'No realizada', tone: 'danger' },
};

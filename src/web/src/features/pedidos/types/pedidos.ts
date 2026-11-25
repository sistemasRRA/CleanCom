import type { Schemas } from '../../../shared/api/client';

export type Card = Schemas['PedidoCardDto'];

export type Etapa = Schemas['PedidoEtapa'];

export type Detail = Schemas['PedidoDetailDto'];

export const ETAPAS: { key: Etapa; name: string }[] = [
  { key: 'Agendado', name: 'Agendado' },
  { key: 'Recolectado', name: 'Recolectado' },
  { key: 'EnPlanta', name: 'En planta' },
  { key: 'Listo', name: 'Listo' },
  { key: 'EntregaConfirmada', name: 'Entrega confirmada' },
  { key: 'EnCamino', name: 'En camino' },
  { key: 'Entregado', name: 'Entregado' },
  { key: 'Cancelado', name: 'Cancelado' },
];

export const etapaName = (e: Etapa) => ETAPAS.find((x) => x.key === e)?.name ?? e;

export const KIND: Record<string, string> = { Recoleccion: 'Recolección', Entrega: 'Entrega' };

export type Turno = Schemas['Turno'];

/** Turnos of an entrega: it has no specific time (the API rejects it). */
export const ENTREGA_TURNOS: { key: Turno; name: string }[] = [
  { key: 'Manana', name: 'Mañana' },
  { key: 'Tarde', name: 'Tarde' },
  { key: 'CualquierHora', name: 'Cualquier hora' },
];

export const TURNO_NAME: Record<Turno, string> = {
  Manana: 'mañana',
  Tarde: 'tarde',
  CualquierHora: 'cualquier hora',
  HoraEspecifica: 'hora específica',
};

export const STATE: Record<Schemas['ParadaState'], string> = {
  Pendiente: 'Pendiente',
  EnCamino: 'En camino',
  EnSitio: 'Llegó',
  Realizada: 'Realizada',
  NoRealizada: 'No realizada',
};

export const STATE_TONE: Record<Schemas['ParadaState'], string> = {
  Pendiente: '',
  EnCamino: 'brand',
  EnSitio: 'brand',
  Realizada: 'success',
  NoRealizada: 'danger',
};

type HistoryKind = Schemas['PedidoEventoDto']['kind'];

/** Color of the dot of each kind of movement in the history. */
export const HISTORY_TONE: Record<HistoryKind, string> = {
  Etapa: 'brand',
  EntregaProgramada: 'success',
  RecoleccionProgramada: 'success',
  NoEntregado: 'danger',
  NoRecolectado: 'danger',
  AtrasoPlanta: 'warning',
  Comentario: 'comment',
  Ticket: '',
};

export const HISTORY_FIRST = 8;

export const historyDay = new Intl.DateTimeFormat('es-MX', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  timeZone: 'America/Mexico_City',
});

export const historyTime = new Intl.DateTimeFormat('es-MX', { hour: 'numeric', minute: '2-digit', timeZone: 'America/Mexico_City' });

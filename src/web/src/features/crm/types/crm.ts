import type { Schemas } from '../../../shared/api/client';

export type Row = Schemas['CrmRowDto'];

export type Status = Schemas['CrmStatus'];

export type Result = Schemas['ContactResult'];

export const STATUS: Record<Status, { name: string; tone: string }> = {
  LeToca: { name: 'Le toca', tone: 'warning' },
  EnRiesgo: { name: 'En riesgo', tone: 'danger' },
  Nuevo: { name: 'Nuevo', tone: 'success' },
  Activo: { name: 'Activo', tone: 'brand' },
  Prospecto: { name: 'Prospecto', tone: '' },
  Inactivo: { name: 'Inactivo', tone: '' },
};

export const STATUS_ORDER: Status[] = ['LeToca', 'EnRiesgo', 'Nuevo', 'Activo', 'Inactivo', 'Prospecto'];

export const RESULTS: { key: Result; name: string; hint: string }[] = [
  { key: 'NoContesto', name: 'No contestó', hint: 'Se vuelve a llamar mañana' },
  { key: 'LlamarDespues', name: 'Pidió que le llamaran', hint: 'En 3 días' },
  { key: 'NoPorAhora', name: 'No por ahora', hint: 'En 3 semanas' },
  { key: 'Agendo', name: 'Agendó', hint: 'Sin seguimiento' },
  { key: 'NoContactar', name: 'No contactar', hint: 'Sale de "Llamar hoy" y campañas' },
];

export const resultName = (r: Result) => RESULTS.find((x) => x.key === r)?.name ?? r;

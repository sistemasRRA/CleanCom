import type { Schemas } from '../../../shared/api/client';

export type ChangeLogPage = Schemas['ChangeLogPageDto'];
export type ChangeLog = Schemas['ChangeLogDto'];
export type ChangeAction = Schemas['ChangeAction'];

export const ACTION: Record<ChangeAction, { name: string; tone: string }> = {
  Alta: { name: 'Alta', tone: 'success' },
  Cambio: { name: 'Cambio', tone: 'brand' },
  Baja: { name: 'Baja', tone: 'danger' },
  Reactivacion: { name: 'Reactivación', tone: 'warning' },
};

/** Kinds of record of the general bitácora, as the API names them. */
export const ENTITIES: { key: string; name: string }[] = [
  { key: 'Invitado', name: 'Invitados y direcciones' },
  { key: 'Pedido', name: 'Pedidos' },
  { key: 'Ruta', name: 'Rutas y zonas' },
  { key: 'KpiGoal', name: 'Metas de KPI' },
  { key: 'User', name: 'Usuarios y su rol' },
  { key: 'Role', name: 'Permisos de roles' },
];

export const PAGE_SIZE = 25;

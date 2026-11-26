import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of KPIs and their goals: the screens never call `api` directly. */
export const kpisApi = {
  /** GET /kpis */
  board: (query: Query<'/kpis', 'get'>) => unwrap(api.GET('/kpis', { params: { query } })),
  /** GET /kpis/{clave}/detalle */
  detail: (clave: string, query: Query<'/kpis/{clave}/detalle', 'get'>) =>
    unwrap(api.GET('/kpis/{clave}/detalle', { params: { path: { clave }, query } })),
  /** GET /kpis/historico */
  history: (query: Query<'/kpis/historico', 'get'>) => unwrap(api.GET('/kpis/historico', { params: { query } })),
  /** POST /kpis/recalcular */
  recalculate: (query: Query<'/kpis/recalcular', 'post'>) => unwrap(api.POST('/kpis/recalcular', { params: { query } })),
  /** GET /metas */
  goals: () => unwrap(api.GET('/metas')),
  /** PUT /metas/{clave} */
  saveGoal: (clave: string, body: Body<'/metas/{clave}', 'put'>) => unwrap(api.PUT('/metas/{clave}', { params: { path: { clave } }, body })),
  /** DELETE /metas/{clave}/{id} */
  removeGoal: (clave: string, id: number) => unwrap(api.DELETE('/metas/{clave}/{id}', { params: { path: { clave, id } } })),
};

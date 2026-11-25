import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of pedidos: the screens never call `api` directly. */
export const pedidosApi = {
  /** GET /evidencias/{id}/archivo */
  evidenciaFile: (id: number) => unwrap(api.GET('/evidencias/{id}/archivo', { params: { path: { id } }, parseAs: 'blob' })),
  /** GET /pedidos */
  list: (query: Query<'/pedidos', 'get'>) => unwrap(api.GET('/pedidos', { params: { query } })),
  /** DELETE /pedidos/{id} */
  remove: (id: number) => unwrap(api.DELETE('/pedidos/{id}', { params: { path: { id } } })),
  /** GET /pedidos/{id} */
  get: (id: number) => unwrap(api.GET('/pedidos/{id}', { params: { path: { id } } })),
  /** POST /pedidos/{id}/comentarios */
  addComentario: (id: number, body: Body<'/pedidos/{id}/comentarios', 'post'>) =>
    unwrap(api.POST('/pedidos/{id}/comentarios', { params: { path: { id } }, body })),
  /** POST /pedidos/{id}/etapa */
  moveEtapa: (id: number, body: Body<'/pedidos/{id}/etapa', 'post'>) => unwrap(api.POST('/pedidos/{id}/etapa', { params: { path: { id } }, body })),
  /** GET /pedidos/{id}/fechas-entrega */
  entregaDates: (id: number) => unwrap(api.GET('/pedidos/{id}/fechas-entrega', { params: { path: { id } } })),
  /** POST /pedidos/{id}/no-entregado */
  markNoEntregado: (id: number, body: Body<'/pedidos/{id}/no-entregado', 'post'>) =>
    unwrap(api.POST('/pedidos/{id}/no-entregado', { params: { path: { id } }, body })),
  /** POST /pedidos/{id}/programar-entrega */
  scheduleEntrega: (id: number, body: Body<'/pedidos/{id}/programar-entrega', 'post'>) =>
    unwrap(api.POST('/pedidos/{id}/programar-entrega', { params: { path: { id } }, body })),
  /** GET /pedidos/tablero */
  board: (query: Query<'/pedidos/tablero', 'get'>) => unwrap(api.GET('/pedidos/tablero', { params: { query } })),
};

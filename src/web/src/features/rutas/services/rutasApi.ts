import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of rutas: the screens never call `api` directly. */
export const rutasApi = {
  /** POST /paradas */
  addParada: (body: Body<'/paradas', 'post'>) => unwrap(api.POST('/paradas', { body })),
  /** DELETE /paradas/{id} */
  removeParada: (id: number) => unwrap(api.DELETE('/paradas/{id}', { params: { path: { id } } })),
  /** PATCH /paradas/{id} */
  updateParada: (id: number, body: Body<'/paradas/{id}', 'patch'>) => unwrap(api.PATCH('/paradas/{id}', { params: { path: { id } }, body })),
  /** PUT /rutas/{rutaId}/dias/{fecha}/chofer */
  assignChofer: (rutaId: number, fecha: string, body: Body<'/rutas/{rutaId}/dias/{fecha}/chofer', 'put'>) =>
    unwrap(api.PUT('/rutas/{rutaId}/dias/{fecha}/chofer', { params: { path: { rutaId, fecha } }, body })),
  /** PUT /rutas/{rutaId}/dias/{fecha}/unidad */
  assignUnidad: (rutaId: number, fecha: string, body: Body<'/rutas/{rutaId}/dias/{fecha}/unidad', 'put'>) =>
    unwrap(api.PUT('/rutas/{rutaId}/dias/{fecha}/unidad', { params: { path: { rutaId, fecha } }, body })),
  /** GET /rutas/dia */
  day: (query: Query<'/rutas/dia', 'get'>) => unwrap(api.GET('/rutas/dia', { params: { query } })),
  /** POST /rutas/dia/{id}/optimizar */
  optimize: (id: number) => unwrap(api.POST('/rutas/dia/{id}/optimizar', { params: { path: { id } } })),
  /** POST /viajes */
  addViaje: (body: Body<'/viajes', 'post'>) => unwrap(api.POST('/viajes', { body })),
};

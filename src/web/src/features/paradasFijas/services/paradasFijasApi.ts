import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of paradasFijas: the screens never call `api` directly. */
export const paradasFijasApi = {
  /** GET /paradas-fijas */
  list: (query: Query<'/paradas-fijas', 'get'>) => unwrap(api.GET('/paradas-fijas', { params: { query } })),
  /** POST /paradas-fijas */
  create: (body: Body<'/paradas-fijas', 'post'>) => unwrap(api.POST('/paradas-fijas', { body })),
  /** GET /paradas-fijas/{id} */
  get: (id: number) => unwrap(api.GET('/paradas-fijas/{id}', { params: { path: { id } } })),
  /** PUT /paradas-fijas/{id} */
  update: (id: number, body: Body<'/paradas-fijas/{id}', 'put'>) => unwrap(api.PUT('/paradas-fijas/{id}', { params: { path: { id } }, body })),
  /** POST /paradas-fijas/{id}/excepciones */
  addException: (id: number, body: Body<'/paradas-fijas/{id}/excepciones', 'post'>) =>
    unwrap(api.POST('/paradas-fijas/{id}/excepciones', { params: { path: { id } }, body })),
  /** DELETE /paradas-fijas/{id}/excepciones/{exceptionId} */
  removeException: (id: number, exceptionId: number) =>
    unwrap(api.DELETE('/paradas-fijas/{id}/excepciones/{exceptionId}', { params: { path: { id, exceptionId } } })),
  /** POST /paradas-fijas/generar */
  generate: (query: Query<'/paradas-fijas/generar', 'post'>) => unwrap(api.POST('/paradas-fijas/generar', { params: { query } })),
  /** GET /paradas-fijas/semana */
  week: (query: Query<'/paradas-fijas/semana', 'get'>) => unwrap(api.GET('/paradas-fijas/semana', { params: { query } })),
};

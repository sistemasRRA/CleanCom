import { api, unwrap } from '../../../shared/api/client';
import type { Query } from '../../../shared/api/types';

/** Calls to the API of the bitácora: the screens never call `api` directly. */
export const bitacoraApi = {
  /** GET /bitacora (SuperUser and Administración) */
  search: (query: Query<'/bitacora', 'get'>) => unwrap(api.GET('/bitacora', { params: { query } })),
  /** GET /invitados/{id}/bitacora */
  invitado: (id: number, query: Query<'/invitados/{id}/bitacora', 'get'>) =>
    unwrap(api.GET('/invitados/{id}/bitacora', { params: { path: { id }, query } })),
};

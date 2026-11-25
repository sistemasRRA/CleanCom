import { api, unwrap } from './client';
import type { Query } from './types';

/** Calls to the API: the screens never call `api` directly. */
export const direccionesApi = {
  /** GET /direcciones/ubicar */
  locate: (query: Query<'/direcciones/ubicar', 'get'>) => unwrap(api.GET('/direcciones/ubicar', { params: { query } })),
};

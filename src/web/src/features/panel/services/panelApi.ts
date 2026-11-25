import { api, unwrap } from '../../../shared/api/client';
import type { Query } from '../../../shared/api/types';

/** Calls to the API of panel: the screens never call `api` directly. */
export const panelApi = {
  /** GET /panel */
  get: (query: Query<'/panel', 'get'>) => unwrap(api.GET('/panel', { params: { query } })),
  /** GET /ubicaciones */
  locations: (query: Query<'/ubicaciones', 'get'>) => unwrap(api.GET('/ubicaciones', { params: { query } })),
};

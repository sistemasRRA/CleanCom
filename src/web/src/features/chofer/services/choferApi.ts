import { api, unwrap } from '../../../shared/api/client';
import type { Query } from '../../../shared/api/types';

/** Calls to the API of chofer: the screens never call `api` directly. */
export const choferApi = {
  /** GET /chofer/vista */
  vista: (query: Query<'/chofer/vista', 'get'>) => unwrap(api.GET('/chofer/vista', { params: { query } })),
};

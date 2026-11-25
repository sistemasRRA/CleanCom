import { api, unwrap } from '@cleancom/mobile-shared';

/** Calls to the API of beneficios: the screens never call `api` directly. */
export const beneficiosApi = {
  /** GET /yo/beneficios */
  mine: () => unwrap(api.GET('/yo/beneficios')),
};

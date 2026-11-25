import { api, unwrap, type ApiBody } from '@cleancom/mobile-shared';

/** Calls to the API of pedir: the screens never call `api` directly. */
export const pedirApi = {
  /** POST /agenda/agendar */
  schedule: (body: ApiBody<'/agenda/agendar', 'post'>) => unwrap(api.POST('/agenda/agendar', { body })),
  /** POST /agenda/disponibilidad */
  availability: (body: ApiBody<'/agenda/disponibilidad', 'post'>) => unwrap(api.POST('/agenda/disponibilidad', { body })),
  /** GET /lineas-servicio */
  lineasServicio: () => unwrap(api.GET('/lineas-servicio')),
};

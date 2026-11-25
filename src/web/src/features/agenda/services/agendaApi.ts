import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of agenda: the screens never call `api` directly. */
export const agendaApi = {
  /** POST /agenda/agendar */
  schedule: (body: Body<'/agenda/agendar', 'post'>) => unwrap(api.POST('/agenda/agendar', { body })),
  /** POST /agenda/disponibilidad */
  availability: (body: Body<'/agenda/disponibilidad', 'post'>) => unwrap(api.POST('/agenda/disponibilidad', { body })),
  /** GET /agenda/tablero */
  board: (query: Query<'/agenda/tablero', 'get'>) => unwrap(api.GET('/agenda/tablero', { params: { query } })),
};

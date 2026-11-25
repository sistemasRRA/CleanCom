import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of planta: the screens never call `api` directly. */
export const plantaApi = {
  /** POST /planta/comentarios */
  addComentario: (body: Body<'/planta/comentarios', 'post'>) => unwrap(api.POST('/planta/comentarios', { body })),
  /** GET /planta/tickets */
  tickets: (query: Query<'/planta/tickets', 'get'>) => unwrap(api.GET('/planta/tickets', { params: { query } })),
  /** POST /planta/tickets */
  captureTicket: (body: Body<'/planta/tickets', 'post'>) => unwrap(api.POST('/planta/tickets', { body })),
  /** POST /planta/tickets/{folio}/listo */
  markReady: (folio: string) => unwrap(api.POST('/planta/tickets/{folio}/listo', { params: { path: { folio } } })),
  /** POST /planta/tickets/{id}/baja */
  removeTicket: (id: number, body: Body<'/planta/tickets/{id}/baja', 'post'>) =>
    unwrap(api.POST('/planta/tickets/{id}/baja', { params: { path: { id } }, body })),
};

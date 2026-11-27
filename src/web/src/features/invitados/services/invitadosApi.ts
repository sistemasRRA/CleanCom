import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of invitados: the screens never call `api` directly. */
export const invitadosApi = {
  /** GET /invitados */
  list: (query: Query<'/invitados', 'get'>) => unwrap(api.GET('/invitados', { params: { query } })),
  /** POST /invitados */
  create: (body: Body<'/invitados', 'post'>) => unwrap(api.POST('/invitados', { body })),
  /** DELETE /invitados/{id} */
  remove: (id: number) => unwrap(api.DELETE('/invitados/{id}', { params: { path: { id } } })),
  /** GET /invitados/{id} */
  get: (id: number) => unwrap(api.GET('/invitados/{id}', { params: { path: { id } } })),
  /** PUT /invitados/{id} */
  update: (id: number, body: Body<'/invitados/{id}', 'put'>) => unwrap(api.PUT('/invitados/{id}', { params: { path: { id } }, body })),
  /** POST /invitados/{id}/contactos */
  addContacto: (id: number, body: Body<'/invitados/{id}/contactos', 'post'>) =>
    unwrap(api.POST('/invitados/{id}/contactos', { params: { path: { id } }, body })),
  /** POST /invitados/{id}/direcciones */
  addDireccion: (id: number, body: Body<'/invitados/{id}/direcciones', 'post'>) =>
    unwrap(api.POST('/invitados/{id}/direcciones', { params: { path: { id } }, body })),
  /** PUT /invitados/{id}/direcciones/{addressId} */
  updateDireccion: (id: number, addressId: number, body: Body<'/invitados/{id}/direcciones/{addressId}', 'put'>) =>
    unwrap(api.PUT('/invitados/{id}/direcciones/{addressId}', { params: { path: { id, addressId } }, body })),
  /** GET /invitados/{id}/ficha */
  ficha: (id: number) => unwrap(api.GET('/invitados/{id}/ficha', { params: { path: { id } } })),
  /** PUT /invitados/{id}/seguimiento */
  updateSeguimiento: (id: number, body: Body<'/invitados/{id}/seguimiento', 'put'>) =>
    unwrap(api.PUT('/invitados/{id}/seguimiento', { params: { path: { id } }, body })),
  /** GET /invitados/{id}/historia */
  historia: (id: number) => unwrap(api.GET('/invitados/{id}/historia', { params: { path: { id } } })),
  /** GET /invitados/{id}/duplicados */
  duplicados: (id: number) => unwrap(api.GET('/invitados/{id}/duplicados', { params: { path: { id } } })),
  /** POST /invitados/{id}/fusionar */
  fusionar: (id: number, body: Body<'/invitados/{id}/fusionar', 'post'>) =>
    unwrap(api.POST('/invitados/{id}/fusionar', { params: { path: { id } }, body })),
  /** POST /invitados/{id}/invitar-app */
  invitarApp: (id: number) => unwrap(api.POST('/invitados/{id}/invitar-app', { params: { path: { id } } })),
  /** POST /invitados/{id}/bloquear-app */
  bloquearApp: (id: number, body: Body<'/invitados/{id}/bloquear-app', 'post'>) =>
    unwrap(api.POST('/invitados/{id}/bloquear-app', { params: { path: { id } }, body })),
  /** GET /invitados/buscar */
  search: (query: Query<'/invitados/buscar', 'get'>) => unwrap(api.GET('/invitados/buscar', { params: { query } })),
  /** GET /invitados/llamar-hoy */
  callToday: (query: Query<'/invitados/llamar-hoy', 'get'>) => unwrap(api.GET('/invitados/llamar-hoy', { params: { query } })),
};

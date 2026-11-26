import { api, unwrap, type ApiBody, type ApiQuery } from '@cleancom/mobile-shared';

/**
 * Calls of the chofer app to the API: screens and the offline queue never call `api` directly. `signal` lets the
 * caller give up on a slow network (the queue retries later).
 */
export const choferApi = {
  /** GET /chofer/ruta */
  ruta: (query: ApiQuery<'/chofer/ruta', 'get'>, signal?: AbortSignal) => unwrap(api.GET('/chofer/ruta', { params: { query }, signal })),
  /** GET /chofer/motivos */
  motivos: () => unwrap(api.GET('/chofer/motivos')),
  /** POST /chofer/paradas/{id}/en-camino */
  enCamino: (id: number, body: ApiBody<'/chofer/paradas/{id}/en-camino', 'post'>, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/paradas/{id}/en-camino', { params: { path: { id } }, body, signal })),
  /** POST /chofer/paradas/{id}/llegue */
  llegue: (id: number, body: ApiBody<'/chofer/paradas/{id}/llegue', 'post'>, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/paradas/{id}/llegue', { params: { path: { id } }, body, signal })),
  /** POST /chofer/paradas/{id}/cerrar */
  cerrar: (id: number, body: ApiBody<'/chofer/paradas/{id}/cerrar', 'post'>, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/paradas/{id}/cerrar', { params: { path: { id } }, body, signal })),
  /** POST /chofer/paradas/{id}/no-realizada */
  noRealizada: (id: number, body: ApiBody<'/chofer/paradas/{id}/no-realizada', 'post'>, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/paradas/{id}/no-realizada', { params: { path: { id } }, body, signal })),
  /** POST /chofer/evidencias/firma */
  firma: (body: ApiBody<'/chofer/evidencias/firma', 'post'>, signal?: AbortSignal) => unwrap(api.POST('/chofer/evidencias/firma', { body, signal })),
  /**
   * POST /chofer/evidencias (multipart: the generated type does not describe the file, so the caller builds the form
   * data; React Native sets the boundary itself, so no Content-Type).
   */
  foto: (form: FormData, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/evidencias', { body: {} as never, bodySerializer: () => form, headers: { 'Content-Type': null }, signal })),
  /** POST /chofer/turno/iniciar */
  iniciarTurno: (body: ApiBody<'/chofer/turno/iniciar', 'post'>, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/turno/iniciar', { body, signal })),
  /** POST /chofer/turno/cerrar */
  cerrarTurno: (body: ApiBody<'/chofer/turno/cerrar', 'post'>, signal?: AbortSignal) => unwrap(api.POST('/chofer/turno/cerrar', { body, signal })),
  /** POST /chofer/fallas (multipart, like a photo: the caller builds the form data). */
  falla: (form: FormData, signal?: AbortSignal) =>
    unwrap(api.POST('/chofer/fallas', { body: {} as never, bodySerializer: () => form, headers: { 'Content-Type': null }, signal })),
  /** POST /chofer/ubicacion */
  ubicacion: (body: ApiBody<'/chofer/ubicacion', 'post'>) => unwrap(api.POST('/chofer/ubicacion', { body })),
};

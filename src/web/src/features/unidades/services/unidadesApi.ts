import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of unidades: the screens never call `api` directly. */
export const unidadesApi = {
  /** GET /avisos-flota/reglas */
  alertRules: () => unwrap(api.GET('/avisos-flota/reglas')),
  /** PUT /avisos-flota/reglas */
  saveAlertRules: (body: Body<'/avisos-flota/reglas', 'put'>) => unwrap(api.PUT('/avisos-flota/reglas', { body })),
  /** GET /unidades */
  list: (query: Query<'/unidades', 'get'>) => unwrap(api.GET('/unidades', { params: { query } })),
  /** POST /unidades */
  create: (body: Body<'/unidades', 'post'>) => unwrap(api.POST('/unidades', { body })),
  /** GET /unidades/{id} */
  get: (id: number) => unwrap(api.GET('/unidades/{id}', { params: { path: { id } } })),
  /** PUT /unidades/{id} */
  update: (id: number, body: Body<'/unidades/{id}', 'put'>) => unwrap(api.PUT('/unidades/{id}', { params: { path: { id } }, body })),
  /** POST /unidades/{id}/documentos (multipart: the generated type does not describe the file, so the caller builds the form data). */
  addDocument: (id: number, data: FormData) =>
    unwrap(api.POST('/unidades/{id}/documentos', { params: { path: { id } }, body: {} as never, bodySerializer: () => data })),
  /** DELETE /unidades/{id}/documentos/{documentoId} */
  removeDocument: (id: number, documentoId: number) =>
    unwrap(api.DELETE('/unidades/{id}/documentos/{documentoId}', { params: { path: { id, documentoId } } })),
  /** GET /unidades/{id}/documentos/{documentoId}/archivo */
  documentFile: (id: number, documentoId: number) =>
    unwrap(api.GET('/unidades/{id}/documentos/{documentoId}/archivo', { params: { path: { id, documentoId } }, parseAs: 'blob' })),
  /** POST /unidades/{id}/eventos */
  addEvento: (id: number, body: Body<'/unidades/{id}/eventos', 'post'>) =>
    unwrap(api.POST('/unidades/{id}/eventos', { params: { path: { id } }, body })),
  /** POST /unidades/{id}/eventos/{eventoId}/resolver */
  resolveEvento: (id: number, eventoId: number) =>
    unwrap(api.POST('/unidades/{id}/eventos/{eventoId}/resolver', { params: { path: { id, eventoId } } })),
  /** POST /unidades/{id}/pagos */
  addPago: (id: number, body: Body<'/unidades/{id}/pagos', 'post'>) => unwrap(api.POST('/unidades/{id}/pagos', { params: { path: { id } }, body })),
  /** PUT /unidades/{id}/pagos/{pagoId} */
  updatePago: (id: number, pagoId: number, body: Body<'/unidades/{id}/pagos/{pagoId}', 'put'>) =>
    unwrap(api.PUT('/unidades/{id}/pagos/{pagoId}', { params: { path: { id, pagoId } }, body })),
};

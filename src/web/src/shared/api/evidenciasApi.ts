import { api, unwrap } from './client';

/** The files of the evidence (Pedidos and Evidencias y caja): the screens never call `api` directly. */
export const evidenciasFilesApi = {
  /** GET /evidencias/{id}/archivo */
  file: (id: number) => unwrap(api.GET('/evidencias/{id}/archivo', { params: { path: { id } }, parseAs: 'blob' })),
};

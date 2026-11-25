import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of avisos: the screens never call `api` directly. */
export const avisosApi = {
  /** GET /avisos */
  list: (query: Query<'/avisos', 'get'>) => unwrap(api.GET('/avisos', { params: { query } })),
  /** GET /avisos-oficina */
  listOficina: () => unwrap(api.GET('/avisos-oficina')),
  /** POST /avisos-oficina/{id}/atendido */
  attendOficina: (id: number) => unwrap(api.POST('/avisos-oficina/{id}/atendido', { params: { path: { id } } })),
  /** POST /avisos/{id}/mandado */
  markSentByHand: (id: number) => unwrap(api.POST('/avisos/{id}/mandado', { params: { path: { id } } })),
  /** GET /plantillas */
  listPlantillas: () => unwrap(api.GET('/plantillas')),
  /** PUT /plantillas/{id} */
  updatePlantilla: (id: number, body: Body<'/plantillas/{id}', 'put'>) => unwrap(api.PUT('/plantillas/{id}', { params: { path: { id } }, body })),
  /** POST /plantillas/{id}/previsualizar */
  previewPlantilla: (id: number, body: Body<'/plantillas/{id}/previsualizar', 'post'>) =>
    unwrap(api.POST('/plantillas/{id}/previsualizar', { params: { path: { id } }, body })),
};

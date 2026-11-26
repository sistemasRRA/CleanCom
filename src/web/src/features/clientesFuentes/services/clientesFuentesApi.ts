import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of Clientes y fuentes: the screens never call `api` directly. */
export const clientesFuentesApi = {
  /** GET /gerencia/clientes */
  period: (query: Query<'/gerencia/clientes', 'get'>) => unwrap(api.GET('/gerencia/clientes', { params: { query } })),
  /** GET /gerencia/clientes/retencion */
  retention: (query: Query<'/gerencia/clientes/retencion', 'get'>) => unwrap(api.GET('/gerencia/clientes/retencion', { params: { query } })),
  /** GET /gerencia/clientes/perdidos */
  lost: (query: Query<'/gerencia/clientes/perdidos', 'get'>) => unwrap(api.GET('/gerencia/clientes/perdidos', { params: { query } })),
  /** POST /gerencia/clientes/perdidos/llamar */
  call: (body: Body<'/gerencia/clientes/perdidos/llamar', 'post'>) => unwrap(api.POST('/gerencia/clientes/perdidos/llamar', { body })),
};

export const clientesFuentesKey = ['clientes-fuentes'] as const;

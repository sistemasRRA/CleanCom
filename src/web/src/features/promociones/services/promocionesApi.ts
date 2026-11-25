import { api, unwrap } from '../../../shared/api/client';
import type { Body } from '../../../shared/api/types';

/** Calls to the API of promociones: the screens never call `api` directly. */
export const promocionesApi = {
  /** POST /beneficios/{codigo}/usar */
  useBeneficio: (codigo: string) => unwrap(api.POST('/beneficios/{codigo}/usar', { params: { path: { codigo } } })),
  /** GET /invitados/{id}/beneficios */
  beneficiosOf: (id: number) => unwrap(api.GET('/invitados/{id}/beneficios', { params: { path: { id } } })),
  /** POST /invitados/{id}/beneficios */
  grantBeneficio: (id: number, body: Body<'/invitados/{id}/beneficios', 'post'>) =>
    unwrap(api.POST('/invitados/{id}/beneficios', { params: { path: { id } }, body })),
  /** GET /promociones */
  list: () => unwrap(api.GET('/promociones')),
  /** POST /promociones */
  create: (body: Body<'/promociones', 'post'>) => unwrap(api.POST('/promociones', { body })),
  /** PUT /promociones/{id} */
  update: (id: number, body: Body<'/promociones/{id}', 'put'>) => unwrap(api.PUT('/promociones/{id}', { params: { path: { id } }, body })),
};

import { api, unwrap, type ApiBody, type ApiQuery } from '@cleancom/mobile-shared';

/** Calls to the API of cuenta: the screens never call `api` directly. */
export const cuentaApi = {
  /** GET /codigos-postales/{code} */
  postalCode: (code: string) => unwrap(api.GET('/codigos-postales/{code}', { params: { path: { code } } })),
  /** GET /direcciones/ubicar */
  locate: (query: ApiQuery<'/direcciones/ubicar', 'get'>) => unwrap(api.GET('/direcciones/ubicar', { params: { query } })),
  /** GET /motivos */
  motivos: (query: ApiQuery<'/motivos', 'get'>) => unwrap(api.GET('/motivos', { params: { query } })),
  /** GET /yo */
  me: () => unwrap(api.GET('/yo')),
  /** PUT /yo */
  updateMe: (body: ApiBody<'/yo', 'put'>) => unwrap(api.PUT('/yo', { body })),
  /** POST /yo/direcciones */
  addDireccion: (body: ApiBody<'/yo/direcciones', 'post'>) => unwrap(api.POST('/yo/direcciones', { body })),
  /** DELETE /yo/direcciones/{addressId} */
  removeDireccion: (addressId: number) => unwrap(api.DELETE('/yo/direcciones/{addressId}', { params: { path: { addressId } } })),
  /** PUT /yo/direcciones/{addressId} */
  updateDireccion: (addressId: number, body: ApiBody<'/yo/direcciones/{addressId}', 'put'>) =>
    unwrap(api.PUT('/yo/direcciones/{addressId}', { params: { path: { addressId } }, body })),
};

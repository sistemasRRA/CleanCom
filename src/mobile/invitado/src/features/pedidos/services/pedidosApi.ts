import { api, unwrap, type ApiBody } from '@cleancom/mobile-shared';

/** Calls to the API of pedidos: the screens never call `api` directly. */
export const pedidosApi = {
  /** GET /yo/pedidos */
  mine: () => unwrap(api.GET('/yo/pedidos')),
  /** GET /yo/pedidos/{id}/cambiar-dia */
  changeDayOptions: (id: number) => unwrap(api.GET('/yo/pedidos/{id}/cambiar-dia', { params: { path: { id } } })),
  /** POST /yo/pedidos/{id}/cambiar-dia */
  changeDay: (id: number, body: ApiBody<'/yo/pedidos/{id}/cambiar-dia', 'post'>) =>
    unwrap(api.POST('/yo/pedidos/{id}/cambiar-dia', { params: { path: { id } }, body })),
  /** POST /yo/pedidos/{id}/cancelar */
  cancel: (id: number, body: ApiBody<'/yo/pedidos/{id}/cancelar', 'post'>) =>
    unwrap(api.POST('/yo/pedidos/{id}/cancelar', { params: { path: { id } }, body })),
  /** GET /yo/pedidos/{id}/seguimiento */
  seguimiento: (id: number) => unwrap(api.GET('/yo/pedidos/{id}/seguimiento', { params: { path: { id } } })),
};

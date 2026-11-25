import { api, unwrap } from '../../../shared/api/client';
import type { Body, Query } from '../../../shared/api/types';

/** Calls to the API of usuarios: the screens never call `api` directly. */
export const usuariosApi = {
  /** GET /operadores */
  listOperadores: (query: Query<'/operadores', 'get'>) => unwrap(api.GET('/operadores', { params: { query } })),
  /** POST /operadores */
  createOperador: (body: Body<'/operadores', 'post'>) => unwrap(api.POST('/operadores', { body })),
  /** PUT /operadores/{id} */
  updateOperador: (id: number, body: Body<'/operadores/{id}', 'put'>) => unwrap(api.PUT('/operadores/{id}', { params: { path: { id } }, body })),
  /** GET /roles */
  listRoles: () => unwrap(api.GET('/roles')),
  /** POST /roles */
  createRole: (body: Body<'/roles', 'post'>) => unwrap(api.POST('/roles', { body })),
  /** DELETE /roles/{id} */
  removeRole: (id: number) => unwrap(api.DELETE('/roles/{id}', { params: { path: { id } } })),
  /** PUT /roles/{id} */
  updateRole: (id: number, body: Body<'/roles/{id}', 'put'>) => unwrap(api.PUT('/roles/{id}', { params: { path: { id } }, body })),
  /** PUT /roles/{id}/permisos */
  updatePermissions: (id: number, body: Body<'/roles/{id}/permisos', 'put'>) =>
    unwrap(api.PUT('/roles/{id}/permisos', { params: { path: { id } }, body })),
  /** GET /roles/pantallas */
  screens: () => unwrap(api.GET('/roles/pantallas')),
  /** GET /usuarios */
  list: (query: Query<'/usuarios', 'get'>) => unwrap(api.GET('/usuarios', { params: { query } })),
  /** POST /usuarios */
  create: (body: Body<'/usuarios', 'post'>) => unwrap(api.POST('/usuarios', { body })),
  /** PUT /usuarios/{id} */
  update: (id: number, body: Body<'/usuarios/{id}', 'put'>) => unwrap(api.PUT('/usuarios/{id}', { params: { path: { id } }, body })),
  /** POST /usuarios/{id}/restablecer */
  resetPassword: (id: number, body: Body<'/usuarios/{id}/restablecer', 'post'>) =>
    unwrap(api.POST('/usuarios/{id}/restablecer', { params: { path: { id } }, body })),
  /** GET /usuarios/roles */
  assignableRoles: () => unwrap(api.GET('/usuarios/roles')),
};

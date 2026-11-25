import { api, unwrap } from './client';
import type { Body } from './types';

/** Calls to the API: the screens never call `api` directly. */
export const authApi = {
  /** POST /auth/cambiar-contrasena */
  changePassword: (body: Body<'/auth/cambiar-contrasena', 'post'>) => unwrap(api.POST('/auth/cambiar-contrasena', { body })),
  /** POST /auth/cerrar */
  signOut: (body: Body<'/auth/cerrar', 'post'>) => api.POST('/auth/cerrar', { body }),
  /** POST /auth/login */
  signIn: (body: Body<'/auth/login', 'post'>) => unwrap(api.POST('/auth/login', { body })),
  /** GET /auth/yo */
  me: () => unwrap(api.GET('/auth/yo')),
};

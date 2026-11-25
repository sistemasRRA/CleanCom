import { api, unwrap } from './client';
import type { ApiBody } from './types';

/** Sign-in calls shared by both apps: the screens never call `api` directly. */
export const authApi = {
  /** POST /auth/login */
  signIn: (body: ApiBody<'/auth/login', 'post'>) => unwrap(api.POST('/auth/login', { body })),
  /** POST /auth/cerrar (best effort: the caller ignores a failure) */
  signOut: (body: ApiBody<'/auth/cerrar', 'post'>) => api.POST('/auth/cerrar', { body }),
  /** GET /auth/yo */
  me: () => unwrap(api.GET('/auth/yo')),
  /** POST /auth/cambiar-contrasena */
  changePassword: (body: ApiBody<'/auth/cambiar-contrasena', 'post'>) => unwrap(api.POST('/auth/cambiar-contrasena', { body })),
  /** POST /auth/codigo: sends the sign-in code (invitado app) */
  requestCode: (body: ApiBody<'/auth/codigo', 'post'>) => unwrap(api.POST('/auth/codigo', { body })),
  /** POST /auth/verificar: checks the code and returns the session (invitado app) */
  verifyCode: (body: ApiBody<'/auth/verificar', 'post'>) => unwrap(api.POST('/auth/verificar', { body })),
};

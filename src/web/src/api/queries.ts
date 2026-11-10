import { useQuery } from '@tanstack/react-query';
import { api, unwrap } from './client';

/** Shared TanStack Query keys, so everything that depends on a catalog can be refetched. */
export const keys = {
  settings: ['settings'] as const,
  settingHistory: (key: string) => ['settings', 'history', key] as const,
  rutas: ['rutas'] as const,
  zonas: ['zonas'] as const,
  lugares: ['lugares'] as const,
  nonWorkingDays: ['non-working-days'] as const,
  coverage: ['coverage'] as const,
  reasons: ['reasons'] as const,
  paradaTypes: ['parada-types'] as const,
  lineasServicio: ['lineas-servicio'] as const,
  users: ['users'] as const,
  operadores: ['operadores'] as const,
  roles: ['roles'] as const,
  assignableRoles: ['users', 'roles'] as const,
  screens: ['roles', 'screens'] as const,
  me: ['me'] as const,
};

export function useRutas() {
  return useQuery({ queryKey: keys.rutas, queryFn: () => unwrap(api.GET('/rutas')) });
}

export function useZonas() {
  return useQuery({
    queryKey: keys.zonas,
    queryFn: () => unwrap(api.GET('/zonas', { params: { query: { includeInactive: true } } })),
  });
}

export function useOperadores() {
  return useQuery({
    queryKey: keys.operadores,
    queryFn: () => unwrap(api.GET('/operadores', { params: { query: { includeInactive: true } } })),
  });
}

export function useRoles() {
  return useQuery({ queryKey: keys.roles, queryFn: () => unwrap(api.GET('/roles')) });
}

/** Screens never change while the app runs. */
export function useScreens() {
  return useQuery({ queryKey: keys.screens, queryFn: () => unwrap(api.GET('/roles/pantallas')), staleTime: Infinity });
}

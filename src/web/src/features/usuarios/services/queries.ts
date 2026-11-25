import { useQuery } from '@tanstack/react-query';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { usuariosApi } from './usuariosApi';

export function useOperadores() {
  return useQuery({
    queryKey: keys.operadores,
    queryFn: () => usuariosApi.listOperadores({ includeInactive: true }),
  });
}

export function useRoles() {
  return useQuery({ queryKey: keys.roles, queryFn: () => usuariosApi.listRoles() });
}

/** Screens never change while the app runs. */
export function useScreens() {
  return useQuery({ queryKey: keys.screens, queryFn: () => usuariosApi.screens(), staleTime: Infinity });
}

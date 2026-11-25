import { useQuery } from '@tanstack/react-query';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { operacionApi } from './operacionApi';

export function useRutas() {
  return useQuery({ queryKey: keys.rutas, queryFn: () => operacionApi.listRutas() });
}

export function useZonas() {
  return useQuery({
    queryKey: keys.zonas,
    queryFn: () => operacionApi.listZonas({ includeInactive: true }),
  });
}

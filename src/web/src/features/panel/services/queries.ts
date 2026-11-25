import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import '../../../shared/api/client';
import type { LiveLocation } from '../../../shared/api/realtime';
import { panelApi } from './panelApi';

export const panelKey = ['panel'] as const;

const liveKey = ['ubicaciones'] as const;

/** Last position per ruta: loaded once, then moved by the hub. */
export function useLiveLocations(date: string) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: [...liveKey, date],
    queryFn: () => panelApi.locations({ fecha: date }),
  });
  const receive = useCallback(
    (l: LiveLocation) => {
      if (l.date !== date) return;
      queryClient.setQueryData<LiveLocation[]>([...liveKey, date], (current = []) => [...current.filter((x) => x.rutaDiaId !== l.rutaDiaId), l]);
    },
    [date, queryClient],
  );
  return { locations: query.data ?? [], receive };
}

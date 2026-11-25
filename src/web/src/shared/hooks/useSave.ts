import { type QueryKey, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNotify } from '../components/Toasts';

/**
 * Mutation that, on success, shows a message and refetches the given queries.
 * Errors stay in `error` so the form can show them.
 */
export function useSave<TVars, TResult = unknown>(
  action: (v: TVars) => Promise<TResult>,
  { refetch, message, onDone }: { refetch: QueryKey[]; message?: string; onDone?: (r: TResult) => void },
) {
  const queryClient = useQueryClient();
  const notify = useNotify();
  return useMutation({
    mutationFn: action,
    onSuccess: async (r) => {
      await Promise.all(refetch.map((k) => queryClient.invalidateQueries({ queryKey: k })));
      if (message) notify(message);
      onDone?.(r);
    },
  });
}

/** One-click mutation (deactivate, delete): errors are shown as a toast. */
export function useAction<TVars>(action: (v: TVars) => Promise<unknown>, refetch: QueryKey[], message: string) {
  const queryClient = useQueryClient();
  const notify = useNotify();
  return useMutation({
    mutationFn: action,
    onSuccess: async () => {
      await Promise.all(refetch.map((k) => queryClient.invalidateQueries({ queryKey: k })));
      notify(message);
    },
    onError: (e) => notify(e.message, true),
  });
}

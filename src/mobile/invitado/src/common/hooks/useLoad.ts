import { useFocusEffect } from 'expo-router';
import { useCallback, useEffect, useRef, useState } from 'react';

export type Loaded<T> = { data: T | null; error: string | null; loading: boolean; reload: () => Promise<void> };

/**
 * Loads data from the API and keeps loading, error and a way to reload. `deps` are the values the request depends on:
 * when they change it asks again, and it is "loading" until the answer for those values arrives (the previous data
 * stays on screen meanwhile). With `reloadOnFocus` it asks each time the screen comes back to the front instead.
 */
export function useLoad<T>(load: () => Promise<T>, deps: unknown[], { reloadOnFocus = false } = {}): Loaded<T> {
  const key = JSON.stringify(deps);
  const [state, setState] = useState<{ key: string | null; data: T | null; error: string | null }>({ key: null, data: null, error: null });
  const [reloading, setReloading] = useState(false);
  const latest = useRef(load);
  useEffect(() => {
    latest.current = load;
  });

  // State changes only when the answer arrives.
  const run = useCallback(
    (forKey: string) =>
      latest
        .current()
        .then(
          (data) => setState({ key: forKey, data, error: null }),
          (e: Error) => setState((s) => ({ key: forKey, data: s.data, error: e.message })),
        )
        .finally(() => setReloading(false)),
    [],
  );

  useEffect(() => {
    if (!reloadOnFocus) void run(key);
  }, [key, run, reloadOnFocus]);

  useFocusEffect(
    useCallback(() => {
      if (reloadOnFocus) void run(key);
    }, [key, run, reloadOnFocus]),
  );

  /** Asks again from a button or after a change: shows it as loading right away. */
  const reload = useCallback(() => {
    setReloading(true);
    return run(key);
  }, [key, run]);

  return { data: state.data, error: state.error, loading: state.key !== key || reloading, reload };
}

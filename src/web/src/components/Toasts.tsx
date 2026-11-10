import { createContext, use, useCallback, useState, type ReactNode } from 'react';

type Toast = { id: number; text: string; error: boolean };
type Notify = (text: string, error?: boolean) => void;

const Context = createContext<Notify>(() => {});

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const notify = useCallback<Notify>((text, error = false) => {
    const id = Date.now() + Math.random();
    setToasts((list) => [...list, { id, text, error }]);
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), error ? 7000 : 3500);
  }, []);

  return (
    <Context value={notify}>
      {children}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast${t.error ? ' error' : ''}`}>
            {t.text}
          </div>
        ))}
      </div>
    </Context>
  );
}

/** Shows a short message at the bottom right. */
export function useNotify() {
  return use(Context);
}

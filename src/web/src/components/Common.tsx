import { useEffect, useRef, type FormEvent, type ReactNode } from 'react';
import { useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useNotify } from './Toasts';

export function PageHeader({ title, description, children }: { title: string; description?: ReactNode; children?: ReactNode }) {
  return (
    <header className="page-header">
      <div>
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </div>
      {children && <div className="actions">{children}</div>}
    </header>
  );
}

/** Query state: loading, error or the content. */
export function QueryState({ loading, error, children }: { loading: boolean; error: Error | null; children: ReactNode }) {
  if (error) return <div className="alert error">{error.message}</div>;
  if (loading) return <div className="empty">Cargando…</div>;
  return <>{children}</>;
}

export function ActiveBadge({ active }: { active: boolean }) {
  return active ? <span className="badge success">Activo</span> : <span className="badge">Inactivo</span>;
}

export function Dialog({
  title,
  onClose,
  onSubmit,
  busy,
  submitText = 'Guardar',
  error,
  children,
}: {
  title: string;
  onClose: () => void;
  onSubmit: () => void;
  busy?: boolean;
  submitText?: string;
  error?: Error | null;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);

  function submit(e: FormEvent) {
    e.preventDefault();
    onSubmit();
  }

  return (
    <dialog ref={ref} onClose={onClose}>
      <form onSubmit={submit}>
        <div className="dialog__header">
          <h2>{title}</h2>
        </div>
        <div className="dialog__body">
          {children}
          {error && <div className="alert error">{error.message}</div>}
        </div>
        <div className="dialog__footer">
          <button type="button" onClick={onClose}>
            Cancelar
          </button>
          <button type="submit" className="primary" disabled={busy}>
            {busy ? 'Guardando…' : submitText}
          </button>
        </div>
      </form>
    </dialog>
  );
}

export function Field({ label, hint, children, wide }: { label: string; hint?: ReactNode; children: ReactNode; wide?: boolean }) {
  return (
    <label className={`field${wide ? ' wide' : ''}`}>
      <span>{label}</span>
      {children}
      {hint && <small>{hint}</small>}
    </label>
  );
}

export function Checkbox({ checked, onChange, children, wide }: { checked: boolean; onChange: (v: boolean) => void; children: ReactNode; wide?: boolean }) {
  return (
    <label className={`checkbox${wide ? ' wide' : ''}`}>
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      {children}
    </label>
  );
}

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

export type Weekday = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';

/** Monday first, with the Spanish names shown on screen. */
export const WEEKDAYS: { key: Weekday; name: string }[] = [
  { key: 'Monday', name: 'Lunes' },
  { key: 'Tuesday', name: 'Martes' },
  { key: 'Wednesday', name: 'Miércoles' },
  { key: 'Thursday', name: 'Jueves' },
  { key: 'Friday', name: 'Viernes' },
  { key: 'Saturday', name: 'Sábado' },
  { key: 'Sunday', name: 'Domingo' },
];

const dateTimeFormat = new Intl.DateTimeFormat('es-MX', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'America/Mexico_City',
});
const dateFormat = new Intl.DateTimeFormat('es-MX', { dateStyle: 'full', timeZone: 'UTC' });

/** UTC instant from the API shown in Mexico City time. */
export function formatDateTime(iso?: string | null) {
  return iso ? dateTimeFormat.format(new Date(iso)) : '—';
}

/** Business date (yyyy-MM-dd) shown without time zone conversion. */
export function formatDate(iso: string) {
  return dateFormat.format(new Date(`${iso}T00:00:00Z`));
}

/** Empty text to null, for optional fields. */
export function optional(text: string) {
  return text.trim() === '' ? null : text.trim();
}

export function optionalNumber(text: string) {
  return text.trim() === '' ? null : Number(text);
}

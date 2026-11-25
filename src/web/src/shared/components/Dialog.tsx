import { type FormEvent, type ReactNode, useEffect, useRef } from 'react';

export function Dialog({
  title,
  onClose,
  onSubmit,
  busy,
  submitText = 'Guardar',
  error,
  children,
  className,
  hideCancel,
}: {
  title: string;
  onClose: () => void;
  onSubmit: () => void;
  busy?: boolean;
  submitText?: string;
  error?: Error | null;
  children: ReactNode;
  /** Extra class for the dialog, e.g. a wider one with a map. */
  className?: string;
  /** A dialog that only shows information: just the submit button ("Cerrar"). */
  hideCancel?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);

  function submit(e: FormEvent) {
    e.preventDefault();
    // React passes a submit on to the forms around it; a dialog opened from another must not submit (close) that one.
    e.stopPropagation();
    onSubmit();
  }

  return (
    <dialog ref={ref} onClose={onClose} className={className}>
      <form onSubmit={submit}>
        <div className="dialog__header">
          <h2>{title}</h2>
        </div>
        <div className="dialog__body">
          {children}
          {error && <div className="alert error">{error.message}</div>}
        </div>
        <div className="dialog__footer">
          {!hideCancel && (
            <button type="button" onClick={onClose}>
              Cancelar
            </button>
          )}
          <button type="submit" className="primary" disabled={busy}>
            {busy ? 'Guardando…' : submitText}
          </button>
        </div>
      </form>
    </dialog>
  );
}

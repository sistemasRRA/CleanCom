import { createContext, use, useCallback, useEffect, useRef, useState, type ReactNode } from 'react';

type Toast = { id: number; text: string };
type Notify = (text: string, error?: boolean) => void;

const Context = createContext<Notify>(() => {});

/** How long the success popup stays when nobody closes it. */
const SUCCESS_MS = 3000;

/**
 * Feedback after an action. A success (a save, an insert, a change) opens a popup in the middle of the screen that
 * closes with "Aceptar", Esc or by itself after a few seconds. An error stays as a red message at the bottom right,
 * so it can be read while fixing the form.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [success, setSuccess] = useState<{ id: number; text: string } | null>(null);

  const notify = useCallback<Notify>((text, error = false) => {
    const id = Date.now() + Math.random();
    if (!error) {
      setSuccess({ id, text });
      return;
    }
    setToasts((list) => [...list, { id, text }]);
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), 7000);
  }, []);
  const closeSuccess = useCallback(() => setSuccess(null), []);

  return (
    <Context value={notify}>
      {children}
      {success && <SuccessPopup key={success.id} text={success.text} onClose={closeSuccess} />}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className="toast error">
            {t.text}
          </div>
        ))}
      </div>
    </Context>
  );
}

function SuccessPopup({ text, onClose }: { text: string; onClose: () => void }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    // Modal so it shows above any dialog that is still open.
    if (d && !d.open) d.showModal();
    const timer = setTimeout(onClose, SUCCESS_MS);
    return () => clearTimeout(timer);
  }, [onClose]);

  return (
    <dialog ref={ref} className="success-popup" onClose={onClose} onClick={(e) => e.target === ref.current && onClose()} aria-labelledby="success-popup-text">
      <div className="success-popup__icon" aria-hidden>
        <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" strokeWidth={2.6} strokeLinecap="round" strokeLinejoin="round">
          <path d="M5 12.5l4.5 4.5L19 7.5" />
        </svg>
      </div>
      <p id="success-popup-text">{text}</p>
      <button type="button" className="primary" onClick={onClose} autoFocus>
        Aceptar
      </button>
    </dialog>
  );
}

/** Confirms an action: a success opens a popup; an error (`true`) shows a red message at the bottom right. */
export function useNotify() {
  return use(Context);
}

// ---------------- Confirm ----------------

export type ConfirmOptions = {
  /** Text of the button that goes ahead, e.g. "Desactivar"; "Sí, continuar" when not given. */
  confirmText?: string;
  /** A red button and icon, for something that removes or gives de baja. */
  danger?: boolean;
  /** A second line under the question. */
  detail?: string;
};
type Ask = (question: string, options?: ConfirmOptions) => Promise<boolean>;
type Pending = ConfirmOptions & { id: number; question: string; resolve: (ok: boolean) => void };

const ConfirmContext = createContext<Ask>(() => Promise.resolve(false));

/** The app's own "¿Seguro?" popup, instead of the browser's window.confirm. */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<Pending | null>(null);
  const ask = useCallback<Ask>(
    (question, options) => new Promise<boolean>((resolve) => setPending({ ...options, id: Date.now(), question, resolve })),
    [],
  );
  const answer = useCallback(
    (ok: boolean) =>
      setPending((p) => {
        p?.resolve(ok);
        return null;
      }),
    [],
  );

  return (
    <ConfirmContext value={ask}>
      {children}
      {pending && <ConfirmPopup key={pending.id} {...pending} onAnswer={answer} />}
    </ConfirmContext>
  );
}

function ConfirmPopup({ question, detail, confirmText = 'Sí, continuar', danger, onAnswer }: Pending & { onAnswer: (ok: boolean) => void }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    // Modal so it shows above the dialog that asked.
    if (d && !d.open) d.showModal();
  }, []);

  return (
    <dialog
      ref={ref}
      className={`confirm-popup${danger ? ' danger' : ''}`}
      onClose={() => onAnswer(false)}
      onClick={(e) => e.target === ref.current && onAnswer(false)}
      aria-labelledby="confirm-popup-text"
    >
      <div className="confirm-popup__icon" aria-hidden>
        <svg viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="currentColor" strokeWidth={2.4} strokeLinecap="round" strokeLinejoin="round">
          {danger ? (
            <path d="M4 7h16M9 7V4.5h6V7M6.5 7l1 13h9l1-13M10 11v5M14 11v5" />
          ) : (
            <path d="M12 8v5M12 16.5v.01M10.3 3.9L2.5 17.5A2 2 0 0 0 4.2 20.5h15.6a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z" />
          )}
        </svg>
      </div>
      <p id="confirm-popup-text">{question}</p>
      {detail && <p className="muted">{detail}</p>}
      <div className="actions">
        {/* Cancel has the focus: Enter by mistake doesn't remove anything. */}
        <button type="button" onClick={() => onAnswer(false)} autoFocus>
          Cancelar
        </button>
        <button type="button" className={danger ? 'danger-solid' : 'primary'} onClick={() => onAnswer(true)}>
          {confirmText}
        </button>
      </div>
    </dialog>
  );
}

/**
 * `const ask = useConfirm();` asks before an action, with the app's popup: `if (await ask('¿Desactivar la zona F1?', { confirmText: 'Desactivar', danger: true })) …`.
 */
export function useConfirm() {
  return use(ConfirmContext);
}

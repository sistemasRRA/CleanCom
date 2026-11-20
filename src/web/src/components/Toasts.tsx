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

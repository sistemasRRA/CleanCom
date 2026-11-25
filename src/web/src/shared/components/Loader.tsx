import { useEffect, useRef, useState, useSyncExternalStore } from 'react';
import { Logo } from './Brand';

/**
 * Small "Cargando…" with the CleanCom mark, for places that load while the user keeps working:
 * searches as you type, panels inside a form, evidence photos and maps.
 */
export function Loader({ label = 'Cargando…', size = 'normal' }: { label?: string; size?: 'normal' | 'small' }) {
  return (
    <div className={`loader loader--${size}`} role="status" aria-live="polite">
      <Logo variant="mark" className="loader__mark" />
      {label && <span className="loader__label">{label}</span>}
    </div>
  );
}

// ---- Full-screen loading ----
// Screens and dialogs report while their data loads; one overlay for the whole app shows the CleanCom logo.

let pending = 0;

const listeners = new Set<() => void>();

const notify = () => listeners.forEach((l) => l());

const subscribe = (l: () => void) => {
  listeners.add(l);
  return () => listeners.delete(l);
};

/** Keeps the full-screen loader up while <paramref name="active"/> is true. */
export function useScreenLoading(active: boolean) {
  useEffect(() => {
    if (!active) return;
    pending++;
    notify();
    return () => {
      pending--;
      notify();
    };
  }, [active]);
}

/** Below this a load is not shown, so fast answers don't flash. Once shown, it stays at least MIN_VISIBLE. */
const SHOW_AFTER = 250;

const MIN_VISIBLE = 500;

/**
 * The full-screen loader, mounted once at the root. It is a modal <dialog> so it also covers other open dialogs
 * (a native modal dialog sits above any z-index), and Esc does not close it.
 */
export function LoadingOverlay() {
  const loading = useSyncExternalStore(subscribe, () => pending > 0);
  const [visible, setVisible] = useState(false);
  const shownAt = useRef(0);

  useEffect(() => {
    if (loading) {
      const t = setTimeout(() => {
        shownAt.current = Date.now();
        setVisible(true);
      }, SHOW_AFTER);
      return () => clearTimeout(t);
    }
    if (!visible) return;
    const t = setTimeout(() => setVisible(false), Math.max(0, MIN_VISIBLE - (Date.now() - shownAt.current)));
    return () => clearTimeout(t);
  }, [loading, visible]);

  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (!d) return;
    if (visible && !d.open) d.showModal();
    if (!visible && d.open) d.close();
  }, [visible]);

  return (
    <dialog ref={ref} className="loading-overlay" onCancel={(e) => e.preventDefault()} aria-label="Cargando">
      <div className="loading-overlay__content" role="status" aria-live="polite">
        <Logo variant="full" className="loading-overlay__logo" />
        <div className="loading-overlay__bar">
          <span />
        </div>
        <span className="loading-overlay__label">Cargando…</span>
      </div>
    </dialog>
  );
}

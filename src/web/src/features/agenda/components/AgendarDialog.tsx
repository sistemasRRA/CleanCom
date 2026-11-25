import { type ReactNode, useEffect, useRef } from 'react';

/**
 * Invitado and "Así se agenda" in a modal: closing it (Cerrar, Esc) drops the invitado without scheduling.
 * Not a <Dialog>: that one wraps its body in a <form>, and Schedule and the address dialogs have their own.
 */
export function AgendarDialog({ onClose, children }: { onClose: () => void; children: ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);

  return (
    <dialog ref={ref} onClose={onClose} className="agendar-dialog">
      <div className="dialog__header agendar-dialog__header">
        <h2>Agendar recolección</h2>
        <button type="button" onClick={onClose}>
          Cerrar sin agendar
        </button>
      </div>
      <div className="dialog__body">{children}</div>
    </dialog>
  );
}

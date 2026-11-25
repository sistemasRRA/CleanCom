import type { Aviso } from '../types/avisos';

/** "Abrir WhatsApp" with the text written, then "Ya lo mandé". */
export function SendByHand({ aviso, canSend, busy, onSent }: { aviso: Aviso; canSend: boolean; busy: boolean; onSent: (id: number) => void }) {
  if (aviso.state !== 'ParaMandarAMano' || !aviso.whatsAppLink) return null;
  return (
    <span className="actions tight" style={{ justifyContent: 'flex-end' }}>
      <a className="button small primary" href={aviso.whatsAppLink} target="_blank" rel="noreferrer">
        Abrir WhatsApp
      </a>
      {canSend && (
        <button type="button" className="small" disabled={busy} onClick={() => onSent(aviso.id)}>
          Ya lo mandé
        </button>
      )}
    </span>
  );
}

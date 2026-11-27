import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { type Invitado } from '../types/invitados';
import { invitadosApi } from '../services/invitadosApi';

/**
 * While capturing: invitados who already have the WhatsApp or the other phone. The same WhatsApp stops the save (the
 * API says so); another shared phone is only a warning. "Usar este invitado" picks the existing one instead.
 */
export function PhoneMatches({
  whatsApp,
  otherPhone,
  exclude,
  onUse,
}: {
  whatsApp: string;
  otherPhone: string;
  exclude?: number;
  onUse?: (i: Invitado) => void;
}) {
  // Asks once the typing stops and there are at least 10 digits; the API normalizes and compares.
  const [phones, setPhones] = useState({ whatsApp: '', otherPhone: '' });
  useEffect(() => {
    const t = setTimeout(() => setPhones({ whatsApp, otherPhone }), 400);
    return () => clearTimeout(t);
  }, [whatsApp, otherPhone]);
  const digits = (s: string) => s.replace(/\D/g, '').length;
  const ready = digits(phones.whatsApp) >= 10 || digits(phones.otherPhone) >= 10;
  const matches = useQuery({
    queryKey: ['invitados', 'telefono', phones, exclude],
    queryFn: () => invitadosApi.phoneMatches({ whatsapp: phones.whatsApp, otro: phones.otherPhone || undefined, excluir: exclude }),
    enabled: ready,
  });
  const [using, setUsing] = useState(false);
  const list = ready ? (matches.data ?? []) : [];
  if (list.length === 0) return null;

  return (
    <div className={`alert stack wide${list.some((m) => m.blocksSave) ? ' error' : ''}`}>
      {list.map((m) => (
        <div key={m.id} className="actions" style={{ justifyContent: 'space-between' }}>
          <span>
            {m.blocksSave ? 'Este WhatsApp ya es de ' : 'Comparte teléfono con '}
            <strong>{m.name}</strong> ({m.whatsAppPhone}
            {m.otherPhone && ` / ${m.otherPhone}`}).{m.blocksSave ? ' Usa su ficha en lugar de capturarlo de nuevo.' : ' Revisa que no sea la misma persona.'}
          </span>
          {onUse && (
            <button
              type="button"
              className="small"
              disabled={using}
              onClick={async () => {
                setUsing(true);
                try {
                  onUse(await invitadosApi.get(m.id));
                } finally {
                  setUsing(false);
                }
              }}
            >
              Usar este invitado
            </button>
          )}
        </div>
      ))}
    </div>
  );
}

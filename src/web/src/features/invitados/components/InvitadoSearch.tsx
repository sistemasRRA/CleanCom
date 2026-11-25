import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import '../../../shared/api/client';
import { Field } from '../../../shared/components/Form';
import { QueryState } from '../../../shared/components/Page';
import { invitadosApi } from '../services/invitadosApi';

// ---- Invitado ----

export function InvitadoSearch({ onChoose, onNew, canEdit }: { onChoose: (id: number) => void; onNew: () => void; canEdit: boolean }) {
  const [text, setText] = useState('');
  const [q, setQ] = useState('');
  useEffect(() => {
    const t = setTimeout(() => setQ(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);

  const results = useQuery({
    queryKey: ['invitados', 'buscar', q],
    queryFn: () => invitadosApi.search({ q }),
    enabled: q.length >= 2,
  });

  return (
    <section className="card">
      <div className="card__header">
        <h2>Invitado</h2>
        {canEdit && (
          <button type="button" onClick={onNew}>
            Nuevo invitado
          </button>
        )}
      </div>
      <div className="card__body stack">
        <Field label="Buscar por nombre o teléfono">
          <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Ana López o 55 1234 5678" autoFocus />
        </Field>
        {q.length >= 2 && (
          <QueryState inline loading={results.isPending} error={results.error}>
            {results.data?.length === 0 ? (
              <div className="muted">Sin resultados. {canEdit && 'Captúralo con "Nuevo invitado".'}</div>
            ) : (
              <ul className="pick-list">
                {results.data?.map((i) => (
                  <li key={i.id}>
                    <button
                      type="button"
                      onClick={() => {
                        onChoose(i.id);
                        setText('');
                      }}
                    >
                      <strong>{i.name}</strong>
                      <span className="muted">
                        {i.whatsAppPhone} · {i.addresses.length} dirección{i.addresses.length === 1 ? '' : 'es'}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </QueryState>
        )}
      </div>
    </section>
  );
}

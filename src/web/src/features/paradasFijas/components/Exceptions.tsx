import { useState } from 'react';
import type { Fija } from '../types/fijas';
import { useNotify } from '../../../shared/components/Toasts';
import { useAction } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { optional } from '../../../shared/utils/format';
import { keys } from '../../../shared/api/queryKeys';
import { paradasFijasApi } from '../services/paradasFijasApi';

/** Dates the parada fija is skipped. */
export function Exceptions({ fija, canEdit }: { fija: Fija; canEdit: boolean }) {
  const [current, setCurrent] = useState(fija.exceptions);
  const [date, setDate] = useState('');
  const [reason, setReason] = useState('');
  const notify = useNotify();
  const add = useAction(
    () =>
      paradasFijasApi.addException(fija.id, { date, reason: optional(reason) }).then((r) => {
        setCurrent(r.exceptions);
        setDate('');
        setReason('');
      }),
    [keys.paradasFijas, keys.agendaBoard],
    'Fecha saltada; ese día sale de la ruta.',
  );
  const remove = useAction(
    (exceptionId: number) => paradasFijasApi.removeException(fija.id, exceptionId).then((r) => setCurrent(r.exceptions)),
    [keys.paradasFijas, keys.agendaBoard],
    'Excepción quitada.',
  );

  return (
    <>
      <h3 className="form-section">Saltar fechas</h3>
      {current.length === 0 ? (
        <p className="muted">Sin fechas saltadas próximas.</p>
      ) : (
        <ul className="plain-list">
          {current.map((e) => (
            <li key={e.id}>
              <strong>{e.label}</strong> {e.reason && <span className="muted">· {e.reason}</span>}
              {canEdit && (
                <button type="button" className="small link" disabled={remove.isPending} onClick={() => remove.mutate(e.id)}>
                  Quitar
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
      {canEdit && (
        <div className="actions tight">
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} style={{ width: 'auto' }} />
          <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Motivo (opcional)" style={{ maxWidth: 220 }} />
          <button
            type="button"
            className="small"
            disabled={!date || add.isPending}
            onClick={() => (date ? add.mutate(undefined) : notify('Elige la fecha.', true))}
          >
            Saltar esta fecha
          </button>
        </div>
      )}
    </>
  );
}

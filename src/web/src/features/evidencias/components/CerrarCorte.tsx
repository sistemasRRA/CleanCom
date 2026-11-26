import { useState } from 'react';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { useSave } from '../../../shared/hooks/useSave';
import { optionalNumber } from '../../../shared/utils/format';
import { evidenciasApi } from '../services/evidenciasApi';
import { cortesKey } from '../services/queryKeys';
import { type Corte, difference, type Method, METHOD_NAME, money } from '../types/evidencias';

/**
 * Closes a corte (POST /cortes/{id}/cerrar): the office captures what the POS says per forma de pago (until the POS is
 * connected) and writes what happened. The API compares, asks for a note when there are differences and gives the folio.
 */
export function CerrarCorte({ corte, onClose }: { corte: Corte; onClose: () => void }) {
  const [pos, setPos] = useState<Record<Method, string>>({ Efectivo: '', Tarjeta: '', Transferencia: '' });
  const [notes, setNotes] = useState('');
  const [invalid, setInvalid] = useState<Error | null>(null);
  const amount = (m: Method) => optionalNumber(pos[m].replace(/[$\s]/g, '').replace(',', '.'));
  const save = useSave(
    () =>
      evidenciasApi.cerrarCorte(corte.corteId!, {
        pos: corte.lines.map((l) => ({ method: l.method, amount: amount(l.method) })),
        notes: notes.trim() || null,
      }),
    { refetch: [cortesKey], message: `Corte de la ruta ${corte.rutaCode} cerrado.`, onDone: onClose },
  );

  function submit() {
    const wrong = corte.lines.filter((l) => Number.isNaN(amount(l.method)));
    if (wrong.length > 0) {
      setInvalid(new Error(`Revisa el monto del punto de venta de ${wrong.map((l) => METHOD_NAME[l.method].toLowerCase()).join(' y ')}.`));
      return;
    }
    setInvalid(null);
    save.mutate(undefined);
  }

  return (
    <Dialog title={`Cerrar corte · Ruta ${corte.rutaCode}`} onClose={onClose} onSubmit={submit} busy={save.isPending} submitText="Cerrar corte" error={invalid ?? save.error}>
      <p className="muted">
        {corte.chofer ?? 'Sin chofer'} · lo que anotó en sus visitas, lo que declaró al cerrar su turno y lo que dice el punto de venta.
      </p>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Forma de pago</th>
              <th className="end">En visitas</th>
              <th className="end">Declarado</th>
              <th className="end">Diferencia</th>
              <th>Punto de venta</th>
            </tr>
          </thead>
          <tbody>
            {corte.lines.map((l) => (
              <tr key={l.method}>
                <td>{METHOD_NAME[l.method]}</td>
                <td className="end">{money(l.registered)}</td>
                <td className="end">{money(l.declared)}</td>
                <td className={`end${l.vsRegistered ? ' caja-diff' : ''}`}>{difference(l.vsRegistered)}</td>
                <td>
                  <input
                    inputMode="decimal"
                    value={pos[l.method]}
                    onChange={(e) => setPos((p) => ({ ...p, [l.method]: e.target.value }))}
                    placeholder="Sin capturar"
                    aria-label={`Punto de venta, ${METHOD_NAME[l.method]}`}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {corte.choferNotes && (
        <p>
          <strong>Nota del chofer:</strong> {corte.choferNotes}
        </p>
      )}
      <Field label="Notas de oficina" hint="Obligatorias si hay diferencias contra las visitas o el punto de venta.">
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={1000} />
      </Field>
    </Dialog>
  );
}

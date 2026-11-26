import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../../../shared/api/client';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { useConfirm, useNotify } from '../../../shared/components/Toasts';
import { optionalNumber } from '../../../shared/utils/format';
import { evidenciasApi } from '../services/evidenciasApi';
import { cortesKey } from '../services/queryKeys';
import { type Corte, type Method, METHOD_NAME, METHODS, money } from '../types/evidencias';

/**
 * The office closes a shift the chofer left open (permiso especial "Cerrar el turno de un chofer desde oficina"): what
 * the chofer handed in, the final km if known and why. The API asks to confirm a long shift or paradas still open.
 */
export function CerrarTurnoOficina({ corte, onClose }: { corte: Corte; onClose: () => void }) {
  const queryClient = useQueryClient();
  const notify = useNotify();
  const ask = useConfirm();
  const [km, setKm] = useState('');
  const [amounts, setAmounts] = useState<Record<Method, string>>({ Efectivo: '', Tarjeta: '', Transferencia: '' });
  const [notes, setNotes] = useState('');
  const amount = (m: Method) => optionalNumber(amounts[m].replace(/[$\s]/g, '').replace(',', '.'));
  const reading = optionalNumber(km.replace(/\D/g, ''));

  const save = useMutation({
    mutationFn: async () => {
      const wrong = METHODS.filter((m) => Number.isNaN(amount(m.key)));
      if (wrong.length > 0) throw new Error(`Revisa el monto de ${wrong.map((m) => m.name.toLowerCase()).join(' y ')}.`);
      const send = (confirm: boolean) =>
        evidenciasApi.cerrarTurno(corte.rutaDiaId, {
          km: reading,
          declared: METHODS.map((m) => ({ method: m.key, amount: amount(m.key) ?? 0 })),
          notes: notes.trim() || null,
          confirm,
        });
      try {
        await send(false);
      } catch (e) {
        // 409 with a question: long shift or paradas still open; the API says what.
        if (!(e instanceof ApiError && e.status === 409 && e.message.includes('¿'))) throw e;
        if (!(await ask(e.message, { confirmText: 'Cerrar el turno' }))) return false;
        await send(true);
      }
      return true;
    },
    onSuccess: async (done) => {
      if (!done) return;
      await queryClient.invalidateQueries({ queryKey: cortesKey });
      notify(`Turno de la ruta ${corte.rutaCode} cerrado.`);
      onClose();
    },
  });

  return (
    <Dialog
      title={`Cerrar turno · Ruta ${corte.rutaCode}`}
      onClose={onClose}
      onSubmit={() => save.mutate()}
      busy={save.isPending}
      submitText="Cerrar turno"
      error={save.error}
    >
      <p className="muted">
        {corte.chofer ?? 'Sin chofer'} no cerró su turno. Captura lo que entregó; el corte queda por revisar. Después ya no podrá trabajar las paradas de ese día.
      </p>
      {corte.openParadas > 0 && (
        <div className="alert warning">
          {corte.openParadas === 1 ? 'Queda 1 parada sin cerrar.' : `Quedan ${corte.openParadas} paradas sin cerrar.`}
        </div>
      )}
      <Field label="Kilometraje final (opcional)" hint={corte.startKm != null ? `Inicial: ${corte.startKm.toLocaleString('es-MX')} km` : undefined}>
        <input inputMode="numeric" value={km} onChange={(e) => setKm(e.target.value)} />
      </Field>
      <div className="form-grid">
        {corte.lines.map((l) => (
          <Field key={l.method} label={`${METHOD_NAME[l.method]} entregado`} hint={`En visitas: ${money(l.registered)}`}>
            <input
              inputMode="decimal"
              value={amounts[l.method]}
              placeholder="$0"
              onChange={(e) => setAmounts((a) => ({ ...a, [l.method]: e.target.value }))}
            />
          </Field>
        ))}
      </div>
      <Field label="¿Por qué lo cierra oficina?">
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} maxLength={500} required />
      </Field>
    </Dialog>
  );
}

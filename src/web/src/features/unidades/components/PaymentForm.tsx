import { useState } from 'react';
import { type Schemas } from '../../../shared/api/client';
import { type Frequency, FREQUENCY } from '../types/unidades';
import { todayInMexicoCity } from '../../../shared/utils/dates';
import { useSave } from '../../../shared/hooks/useSave';
import { detailKey, listKey } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { unidadesApi } from '../services/unidadesApi';

export function PaymentForm({
  unidadId,
  payment,
  onClose,
}: {
  unidadId: number;
  payment: Schemas['RecurringPaymentDto'] | null;
  onClose: () => void;
}) {
  const [f, setF] = useState({
    concept: payment?.concept ?? '',
    amount: payment ? String(payment.amount) : '',
    frequency: payment?.frequency ?? ('Anual' as Frequency),
    nextDate: payment?.nextDate ?? todayInMexicoCity(),
    active: payment?.active ?? true,
  });
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const body: Schemas['SaveRecurringPaymentRequest'] = {
    concept: f.concept,
    amount: Number(f.amount),
    frequency: f.frequency,
    nextDate: f.nextDate,
    active: f.active,
  };
  const save = useSave(() => (payment ? unidadesApi.updatePago(unidadId, payment.id, body) : unidadesApi.addPago(unidadId, body)), {
    refetch: [listKey, detailKey(unidadId)],
    message: 'Pago recurrente guardado.',
    onDone: onClose,
  });
  return (
    <Dialog
      title={payment ? 'Editar pago recurrente' : 'Nuevo pago recurrente'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Concepto" wide>
          <input
            value={f.concept}
            onChange={(e) => set({ concept: e.target.value })}
            required
            maxLength={150}
            placeholder="Tenencia, crédito, póliza en pagos…"
          />
        </Field>
        <Field label="Monto">
          <input type="number" step="0.01" min={0.01} value={f.amount} onChange={(e) => set({ amount: e.target.value })} required />
        </Field>
        <Field label="Periodicidad">
          <select value={f.frequency} onChange={(e) => set({ frequency: e.target.value as Frequency })}>
            {(Object.keys(FREQUENCY) as Frequency[]).map((k) => (
              <option key={k} value={k}>
                {FREQUENCY[k]}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Próxima fecha">
          <input type="date" value={f.nextDate} onChange={(e) => set({ nextDate: e.target.value })} required />
        </Field>
        {payment && (
          <Checkbox checked={!f.active} onChange={(v) => set({ active: !v })} wide>
            Ya no se paga (dar de baja)
          </Checkbox>
        )}
      </div>
    </Dialog>
  );
}

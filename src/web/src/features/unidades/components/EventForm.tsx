import { useState } from 'react';
import { type Detail, EVENT, type EventKind, km, type Severity, SEVERITY } from '../types/unidades';
import { todayInMexicoCity } from '../../../shared/utils/dates';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { optional, optionalNumber } from '../../../shared/utils/format';
import { detailKey, listKey } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { unidadesApi } from '../services/unidadesApi';

export function EventForm({ detail, onClose }: { detail: Detail; onClose: () => void }) {
  const id = detail.card.id;
  const [f, setF] = useState({
    kind: 'Servicio' as EventKind,
    date: todayInMexicoCity(),
    km: '',
    amount: '',
    description: '',
    severity: 'Media' as Severity,
  });
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const save = useSave(
    () =>
      unidadesApi.addEvento(id, {
        kind: f.kind,
        date: f.date,
        km: optionalNumber(f.km),
        amount: optionalNumber(f.amount),
        description: optional(f.description),
        severity: f.kind === 'Falla' ? f.severity : null,
        recurringPaymentId: null,
      }),
    { refetch: [listKey, detailKey(id)], message: 'Registrado en la bitácora.', onDone: onClose },
  );
  return (
    <Dialog
      title={`Registrar en ${detail.card.plate}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Tipo">
          <select value={f.kind} onChange={(e) => set({ kind: e.target.value as EventKind })}>
            {(Object.keys(EVENT) as EventKind[]).map((k) => (
              <option key={k} value={k}>
                {EVENT[k]}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Fecha">
          <input type="date" value={f.date} max={todayInMexicoCity()} onChange={(e) => set({ date: e.target.value })} required />
        </Field>
        <Field label="Kilometraje" hint={f.kind === 'Kilometraje' ? 'Obligatorio.' : `Actual: ${km(detail.card.km)}.`}>
          <input type="number" value={f.km} min={0} onChange={(e) => set({ km: e.target.value })} required={f.kind === 'Kilometraje'} />
        </Field>
        {f.kind !== 'Kilometraje' && (
          <Field label="Monto">
            <input type="number" step="0.01" min={0} value={f.amount} onChange={(e) => set({ amount: e.target.value })} />
          </Field>
        )}
        {f.kind === 'Falla' && (
          <Field label="Gravedad">
            <select value={f.severity} onChange={(e) => set({ severity: e.target.value as Severity })}>
              {(Object.keys(SEVERITY) as Severity[]).map((s) => (
                <option key={s} value={s}>
                  {SEVERITY[s]}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Descripción" wide>
          <textarea
            value={f.description}
            onChange={(e) => set({ description: e.target.value })}
            maxLength={1000}
            rows={2}
            required={f.kind === 'Falla' || f.kind === 'Reparacion'}
          />
        </Field>
      </div>
    </Dialog>
  );
}

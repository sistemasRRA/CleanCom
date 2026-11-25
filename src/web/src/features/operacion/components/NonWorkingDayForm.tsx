import { useState } from 'react';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function NonWorkingDayForm({ onClose }: { onClose: () => void }) {
  const [date, setDate] = useState('');
  const [reason, setReason] = useState('');
  const save = useSave(() => operacionApi.createNonWorkingDay({ date, reason: reason.trim() }), {
    refetch: [keys.nonWorkingDays],
    message: 'Día inhábil agregado.',
    onDone: onClose,
  });
  return (
    <Dialog title="Agregar día inhábil" onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <Field label="Fecha">
        <input type="date" autoFocus value={date} onChange={(e) => setDate(e.target.value)} required />
      </Field>
      <Field label="Motivo">
        <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Día de muertos" required />
      </Field>
    </Dialog>
  );
}

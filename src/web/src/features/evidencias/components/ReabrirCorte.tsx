import { useState } from 'react';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { useSave } from '../../../shared/hooks/useSave';
import { evidenciasApi } from '../services/evidenciasApi';
import { cortesKey } from '../services/queryKeys';
import type { Corte } from '../types/evidencias';

/** Reopens a closed corte (permiso especial "Reabrir un corte de caja cerrado"); the reason is kept with who did it. */
export function ReabrirCorte({ corte, onClose }: { corte: Corte; onClose: () => void }) {
  const [reason, setReason] = useState('');
  const save = useSave(() => evidenciasApi.reabrirCorte(corte.corteId!, { reason: reason.trim() || null }), {
    refetch: [cortesKey],
    message: `Corte ${corte.folio} reabierto.`,
    onDone: onClose,
  });
  return (
    <Dialog
      title={`Reabrir corte ${corte.folio}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      submitText="Reabrir"
      error={save.error}
    >
      <p className="muted">El corte vuelve a "Por revisar" y se compara otra vez contra las visitas; al cerrarlo de nuevo recibe su folio.</p>
      <Field label="¿Por qué se reabre?">
        <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} maxLength={500} required />
      </Field>
    </Dialog>
  );
}

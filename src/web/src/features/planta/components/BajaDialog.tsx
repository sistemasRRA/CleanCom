import { useState } from 'react';
import type { Ticket } from '../types/planta';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { refetch } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { plantaApi } from '../services/plantaApi';

export function BajaDialog({ ticket, onClose }: { ticket: Ticket; onClose: () => void }) {
  const [reason, setReason] = useState('');
  const save = useSave(() => plantaApi.removeTicket(ticket.id, { reason: reason.trim() }), {
    refetch,
    message: `Ticket ${ticket.folio} dado de baja.`,
    onDone: onClose,
  });
  return (
    <Dialog
      title={`Dar de baja el ticket ${ticket.folio}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      submitText="Dar de baja"
      error={save.error}
    >
      <p className="muted">Solo para tickets capturados por error. Queda en el historial del pedido.</p>
      <Field label="Motivo" wide>
        <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Era de otro pedido" autoFocus />
      </Field>
    </Dialog>
  );
}

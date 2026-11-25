import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import type { Card } from '../types/planta';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import { refetch } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { plantaApi } from '../services/plantaApi';
import { operacionApi } from '../../operacion';

export function CommentDialog({ card, onClose }: { card: Card; onClose: () => void }) {
  const reasons = useQuery({ queryKey: keys.reasons, queryFn: () => operacionApi.listMotivos() });
  const delay = reasons.data?.filter((r) => r.type === 'PlantaDelay' && r.active) ?? [];
  const [reasonId, setReasonId] = useState('');
  const [note, setNote] = useState('');
  const save = useSave(
    () => plantaApi.addComentario({ pedidoId: card.pedido.id, reasonId: reasonId ? Number(reasonId) : null, note: optional(note) }),
    { refetch, message: 'Comentario guardado; se ve en Pedidos.', onDone: onClose },
  );
  return (
    <Dialog
      title={`¿Por qué se atrasa? · Pedido #${card.pedido.folio}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        {delay.length > 0 && (
          <Field label="Motivo">
            <select value={reasonId} onChange={(e) => setReasonId(e.target.value)}>
              <option value="">Elige el motivo</option>
              {delay.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.text}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label={delay.length > 0 ? 'Detalle' : 'Comentario'} wide={delay.length === 0}>
          <input value={note} onChange={(e) => setNote(e.target.value)} placeholder="Falta un botón en el saco" />
        </Field>
      </div>
    </Dialog>
  );
}

import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import { refetch } from '../services/queryKeys';
import { Field } from '../../../shared/components/Form';
import { pedidosApi } from '../services/pedidosApi';
import { operacionApi } from '../../operacion';

export function NoEntregadoForm({ id, onDone }: { id: number; onDone: () => void }) {
  const reasons = useQuery({ queryKey: keys.reasons, queryFn: () => operacionApi.listMotivos() });
  const failed = reasons.data?.filter((r) => r.type === 'FailedEntrega' && r.active) ?? [];
  const [reasonId, setReasonId] = useState('');
  const [note, setNote] = useState('');
  const save = useSave(() => pedidosApi.markNoEntregado(id, { reasonId: reasonId ? Number(reasonId) : null, note: optional(note) }), {
    refetch,
    message: 'Intento registrado y entrega reprogramada.',
    onDone,
  });
  return (
    <div className="inline-form">
      <p className="muted">Queda en el historial y se reprograma al siguiente día con cupo de su zona, desde mañana.</p>
      <div className="form-grid">
        {failed.length > 0 && (
          <Field label="Motivo">
            <select value={reasonId} onChange={(e) => setReasonId(e.target.value)}>
              <option value="">Elige el motivo</option>
              {failed.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.text}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label={failed.length > 0 ? 'Detalle' : 'Motivo'} wide={failed.length === 0}>
          <input value={note} onChange={(e) => setNote(e.target.value)} placeholder="No había nadie" />
        </Field>
      </div>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending} onClick={() => save.mutate(undefined)}>
        Registrar y reprogramar
      </button>
    </div>
  );
}

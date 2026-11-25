import { useState } from 'react';
import { type Detail, type Etapa, ETAPAS } from '../types/pedidos';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { optional } from '../../../shared/utils/format';
import { refetch } from '../services/queryKeys';
import { Field } from '../../../shared/components/Form';
import { pedidosApi } from '../services/pedidosApi';

export function MoveForm({ detail: d, onDone }: { detail: Detail; onDone: () => void }) {
  const [etapa, setEtapa] = useState<Etapa>(d.card.etapa);
  const [reason, setReason] = useState('');
  const save = useSave(() => pedidosApi.moveEtapa(d.card.id, { etapa, reason: optional(reason) }), { refetch, message: 'Etapa cambiada.', onDone });
  return (
    <div className="inline-form">
      <p className="muted">Las etapas avanzan solas; mover a mano es para excepciones. Regresar, entregar o cancelar a mano pide motivo.</p>
      <div className="form-grid">
        <Field label="Nueva etapa">
          <select value={etapa} onChange={(e) => setEtapa(e.target.value as Etapa)}>
            {ETAPAS.map((e) => (
              <option key={e.key} value={e.key}>
                {e.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Motivo">
          <input value={reason} onChange={(e) => setReason(e.target.value)} />
        </Field>
      </div>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending || etapa === d.card.etapa} onClick={() => save.mutate(undefined)}>
        Guardar etapa
      </button>
    </div>
  );
}

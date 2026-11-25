import { useState } from 'react';
import { type Schemas } from '../../../shared/api/client';
import { type Result, RESULTS } from '../types/crm';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { invitadosApi } from '../../invitados';

export function ContactForm({ invitadoId, name, onClose }: { invitadoId: number; name: string; onClose: () => void }) {
  const [channel, setChannel] = useState<Schemas['ContactChannel']>('Llamada');
  const [result, setResult] = useState<Result>('NoContesto');
  const [note, setNote] = useState('');
  const [next, setNext] = useState('');
  const save = useSave(() => invitadosApi.addContacto(invitadoId, { channel, result, note: optional(note), nextContactOn: next || null }), {
    refetch: [keys.crm],
    message: 'Contacto registrado.',
    onDone: onClose,
  });
  return (
    <Dialog
      title={`Registrar contacto con ${name}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Canal">
        <div className="segmented">
          {(['Llamada', 'WhatsApp'] as const).map((c) => (
            <button key={c} type="button" className={`segmented__option${channel === c ? ' selected' : ''}`} onClick={() => setChannel(c)}>
              {c}
            </button>
          ))}
        </div>
      </Field>
      <Field label="Resultado">
        <select value={result} onChange={(e) => setResult(e.target.value as Result)}>
          {RESULTS.map((r) => (
            <option key={r.key} value={r.key}>
              {r.name} — {r.hint}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Volver a contactar el" hint="Vacío = según el resultado.">
        <input type="date" value={next} onChange={(e) => setNext(e.target.value)} />
      </Field>
      <Field label="Nota">
        <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={3} maxLength={1000} />
      </Field>
    </Dialog>
  );
}

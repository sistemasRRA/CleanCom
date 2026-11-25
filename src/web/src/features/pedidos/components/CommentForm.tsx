import { useState } from 'react';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { refetch } from '../services/queryKeys';
import { Field } from '../../../shared/components/Form';
import { pedidosApi } from '../services/pedidosApi';

export function CommentForm({ id, onDone }: { id: number; onDone: () => void }) {
  const [note, setNote] = useState('');
  const save = useSave(() => pedidosApi.addComentario(id, { note: note.trim() }), {
    refetch,
    message: 'Comentario guardado.',
    onDone,
  });
  return (
    <div className="inline-form">
      <Field label="Comentario" wide>
        <input value={note} onChange={(e) => setNote(e.target.value)} />
      </Field>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending || !note.trim()} onClick={() => save.mutate(undefined)}>
        Guardar comentario
      </button>
    </div>
  );
}

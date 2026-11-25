import { useState } from 'react';
import { type Schemas } from '../../../shared/api/client';
import { type Status, STATUS, STATUS_ORDER } from '../types/crm';
import { useSave } from '../../../shared/hooks/useSave';
import { keys } from '../../../shared/api/queryKeys';
import { Checkbox, Field } from '../../../shared/components/Form';
import { invitadosApi } from '../../invitados';

export function Seguimiento({ ficha }: { ficha: Schemas['CrmFichaDto'] }) {
  const [manual, setManual] = useState<Status | ''>(ficha.manualStatus ?? '');
  const [frequency, setFrequency] = useState(ficha.fixedFrequencyDays?.toString() ?? '');
  const [next, setNext] = useState(ficha.crm.nextContactOn ?? '');
  const [doNotContact, setDoNotContact] = useState(ficha.crm.doNotContact);
  const save = useSave(
    () =>
      invitadosApi.updateSeguimiento(ficha.invitado.id, {
        manualStatus: manual || null,
        fixedFrequencyDays: frequency ? Number(frequency) : null,
        nextContactOn: next || null,
        doNotContact,
      }),
    { refetch: [keys.crm], message: 'Seguimiento guardado.' },
  );
  return (
    <form
      className="stack card__body seguimiento"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate(undefined);
      }}
    >
      <h3>Seguimiento</h3>
      <div className="form-grid">
        <Field label="Estatus" hint="Vacío = automático">
          <select value={manual} onChange={(e) => setManual(e.target.value as Status | '')}>
            <option value="">Automático</option>
            {STATUS_ORDER.map((s) => (
              <option key={s} value={s}>
                {STATUS[s].name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Frecuencia fija (días)" hint="Vacío = calculada">
          <input type="number" min={1} max={365} value={frequency} onChange={(e) => setFrequency(e.target.value)} />
        </Field>
        <Field label="Próximo contacto">
          <input type="date" value={next} onChange={(e) => setNext(e.target.value)} />
        </Field>
      </div>
      <Checkbox checked={doNotContact} onChange={setDoNotContact}>
        No contactar (pidió no recibir llamadas ni campañas)
      </Checkbox>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <div className="actions end">
        <button type="submit" className="primary" disabled={save.isPending}>
          {save.isPending ? 'Guardando…' : 'Guardar seguimiento'}
        </button>
      </div>
    </form>
  );
}

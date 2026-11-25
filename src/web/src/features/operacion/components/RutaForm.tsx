import { useState } from 'react';
import { type Ruta, shortTime } from '../types/rutasZonas';
import { useSave } from '../../../shared/hooks/useSave';
import { optionalNumber } from '../../../shared/utils/format';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function RutaForm({ ruta, onClose }: { ruta: Ruta | null; onClose: () => void }) {
  const [f, setF] = useState({
    code: ruta?.code ?? '',
    name: ruta?.name ?? '',
    color: ruta?.color ?? '#2a9d8f',
    cupo: ruta?.cupoParadas?.toString() ?? '',
    goal: ruta?.recoleccionGoal?.toString() ?? '',
    maxReturn: shortTime(ruta?.maxReturnTime) ?? '',
    maxReturnSaturday: shortTime(ruta?.maxReturnTimeSaturday) ?? '',
    active: ruta?.active ?? true,
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));

  const save = useSave(
    () => {
      const body = {
        code: f.code.trim(),
        name: f.name.trim(),
        color: f.color,
        cupoParadas: optionalNumber(f.cupo),
        recoleccionGoal: optionalNumber(f.goal),
        maxReturnTime: f.maxReturn ? `${f.maxReturn}:00` : null,
        maxReturnTimeSaturday: f.maxReturnSaturday ? `${f.maxReturnSaturday}:00` : null,
        active: f.active,
      };
      return ruta ? operacionApi.updateRuta(ruta.id, body) : operacionApi.createRuta(body);
    },
    { refetch: [keys.rutas, keys.zonas], message: ruta ? 'Ruta actualizada.' : 'Ruta creada.', onDone: onClose },
  );

  return (
    <Dialog
      title={ruta ? `Ruta ${ruta.code}` : 'Nueva ruta'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Código">
          <input autoFocus value={f.code} onChange={(e) => change({ code: e.target.value })} required />
        </Field>
        <Field label="Nombre">
          <input value={f.name} onChange={(e) => change({ name: e.target.value })} required />
        </Field>
        <Field label="Color">
          <input type="color" value={f.color} onChange={(e) => change({ color: e.target.value })} />
        </Field>
        <Field label="Cupo de paradas">
          <input type="number" min={1} value={f.cupo} onChange={(e) => change({ cupo: e.target.value })} placeholder="general" />
        </Field>
        <Field label="Meta de recolecciones">
          <input type="number" min={1} value={f.goal} onChange={(e) => change({ goal: e.target.value })} placeholder="general" />
        </Field>
        <Field label="Regreso máximo">
          <input type="time" value={f.maxReturn} onChange={(e) => change({ maxReturn: e.target.value })} />
        </Field>
        <Field label="Regreso máximo sábado">
          <input type="time" value={f.maxReturnSaturday} onChange={(e) => change({ maxReturnSaturday: e.target.value })} />
        </Field>
        <Checkbox wide checked={f.active} onChange={(active) => change({ active })}>
          Activa
        </Checkbox>
      </div>
    </Dialog>
  );
}

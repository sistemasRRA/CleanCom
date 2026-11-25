import { useState } from 'react';
import type { Operador } from '../types/users';
import { useRutas } from '../../operacion';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { usuariosApi } from '../services/usuariosApi';

export function OperadorForm({ operador, onClose }: { operador: Operador | null; onClose: () => void }) {
  const rutas = useRutas();
  const [name, setName] = useState(operador?.name ?? '');
  const [phone, setPhone] = useState(operador?.phone ?? '');
  const [rutaId, setRutaId] = useState(operador?.usualRutaId?.toString() ?? '');
  const [active, setActive] = useState(operador?.active ?? true);
  const save = useSave(
    () => {
      const body = { name: name.trim(), phone: optional(phone), usualRutaId: rutaId ? Number(rutaId) : null, active };
      return operador ? usuariosApi.updateOperador(operador.id, body) : usuariosApi.createOperador(body);
    },
    { refetch: [keys.operadores, keys.users], message: 'Operador guardado.', onDone: onClose },
  );
  return (
    <Dialog
      title={operador ? `Editar ${operador.name}` : 'Nuevo operador'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Nombre" wide>
          <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required />
        </Field>
        <Field label="Teléfono">
          <input type="tel" value={phone} onChange={(e) => setPhone(e.target.value)} />
        </Field>
        <Field label="Ruta habitual">
          <select value={rutaId} onChange={(e) => setRutaId(e.target.value)}>
            <option value="">Sin ruta</option>
            {rutas.data?.map((r) => (
              <option key={r.id} value={r.id}>
                {r.code} · {r.name}
              </option>
            ))}
          </select>
        </Field>
        <Checkbox wide checked={active} onChange={setActive}>
          Activo
        </Checkbox>
      </div>
    </Dialog>
  );
}

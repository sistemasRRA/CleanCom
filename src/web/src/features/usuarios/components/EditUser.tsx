import { useState } from 'react';
import type { User } from '../types/users';
import { useOperadores } from '../services/queries';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { optional } from '../../../shared/utils/format';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { RoleOptions } from './RoleOptions';
import { usuariosApi } from '../services/usuariosApi';

export function EditUser({ user, onClose }: { user: User; onClose: () => void }) {
  const operadores = useOperadores();
  const [f, setF] = useState({
    fullName: user.fullName,
    email: user.email ?? '',
    phone: user.phone ?? '',
    role: user.role,
    active: user.active,
    operadorId: user.operadorId?.toString() ?? '',
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const options = operadores.data?.filter((o) => o.active && (!o.userId || o.userId === user.id)) ?? [];

  const save = useSave(
    () =>
      usuariosApi.update(user.id, {
        fullName: f.fullName.trim(),
        email: optional(f.email),
        phone: optional(f.phone),
        role: f.role,
        active: f.active,
        operadorId: f.role === 'Chofer' && f.operadorId ? Number(f.operadorId) : null,
      }),
    { refetch: [keys.users, keys.operadores], message: 'Usuario actualizado.', onDone: onClose },
  );

  return (
    <Dialog title={`Editar ${user.userName}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Nombre completo" wide>
          <input autoFocus value={f.fullName} onChange={(e) => change({ fullName: e.target.value })} required />
        </Field>
        <Field label="Rol">
          <select value={f.role} onChange={(e) => change({ role: e.target.value })}>
            <RoleOptions />
          </select>
        </Field>
        <Field label="Teléfono">
          <input type="tel" value={f.phone} onChange={(e) => change({ phone: e.target.value })} />
        </Field>
        <Field label="Correo" wide>
          <input type="email" value={f.email} onChange={(e) => change({ email: e.target.value })} />
        </Field>
        {f.role === 'Chofer' && (
          <Field label="Operador vinculado" wide>
            <select value={f.operadorId} onChange={(e) => change({ operadorId: e.target.value })}>
              <option value="">Sin operador</option>
              {options.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.name}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Checkbox wide checked={f.active} onChange={(active) => change({ active })}>
          Activo
        </Checkbox>
      </div>
      {(f.role !== user.role || (!f.active && user.active)) && (
        <div className="alert">Al guardar se cierran las sesiones abiertas de este usuario.</div>
      )}
    </Dialog>
  );
}

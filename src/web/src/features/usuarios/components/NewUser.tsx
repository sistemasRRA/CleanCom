import { useState } from 'react';
import { useRutas } from '../../operacion';
import { useOperadores } from '../services/queries';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { optional } from '../../../shared/utils/format';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { RoleOptions } from './RoleOptions';
import { usuariosApi } from '../services/usuariosApi';

export function NewUser({ onClose }: { onClose: () => void }) {
  const rutas = useRutas();
  const operadores = useOperadores();
  const [f, setF] = useState({
    fullName: '',
    userName: '',
    email: '',
    phone: '',
    role: 'Chofer',
    initialPassword: '',
    /** "new", "none" or the id of an existing operador. */
    operador: 'new',
    usualRutaId: '',
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const isChofer = f.role === 'Chofer';
  const unlinked = operadores.data?.filter((o) => o.active && !o.userId) ?? [];

  const save = useSave(
    () =>
      usuariosApi.create({
        fullName: f.fullName.trim(),
        userName: f.userName.trim(),
        email: optional(f.email),
        phone: optional(f.phone),
        role: f.role,
        initialPassword: f.initialPassword,
        operadorId: isChofer && f.operador !== 'new' && f.operador !== 'none' ? Number(f.operador) : null,
        createOperador: isChofer && f.operador === 'new',
        usualRutaId: isChofer && f.operador === 'new' && f.usualRutaId ? Number(f.usualRutaId) : null,
      }),
    { refetch: [keys.users, keys.operadores], message: 'Usuario creado. Cambiará su contraseña al entrar.', onDone: onClose },
  );

  return (
    <Dialog title="Nuevo usuario" onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Nombre completo" wide>
          <input autoFocus value={f.fullName} onChange={(e) => change({ fullName: e.target.value })} required />
        </Field>
        <Field label="Rol">
          <select value={f.role} onChange={(e) => change({ role: e.target.value })}>
            <RoleOptions />
          </select>
        </Field>
        <Field label="Usuario" hint={isChofer ? 'Corto, por ejemplo juan.rosales.' : undefined}>
          <input autoComplete="off" value={f.userName} onChange={(e) => change({ userName: e.target.value })} required />
        </Field>
        <Field label="Teléfono">
          <input type="tel" value={f.phone} onChange={(e) => change({ phone: e.target.value })} />
        </Field>
        <Field label="Correo">
          <input type="email" value={f.email} onChange={(e) => change({ email: e.target.value })} />
        </Field>
        <Field label="Contraseña inicial" hint="Mínimo 8 caracteres. La cambia en su primer acceso." wide>
          <input
            type="text"
            autoComplete="new-password"
            minLength={8}
            value={f.initialPassword}
            onChange={(e) => change({ initialPassword: e.target.value })}
            required
          />
        </Field>
        {isChofer && (
          <>
            <Field label="Operador vinculado">
              <select value={f.operador} onChange={(e) => change({ operador: e.target.value })}>
                <option value="new">Crear operador nuevo</option>
                <option value="none">Sin operador</option>
                {unlinked.map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.name}
                  </option>
                ))}
              </select>
            </Field>
            {f.operador === 'new' && (
              <Field label="Ruta habitual">
                <select value={f.usualRutaId} onChange={(e) => change({ usualRutaId: e.target.value })}>
                  <option value="">Sin ruta</option>
                  {rutas.data
                    ?.filter((r) => r.active)
                    .map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.code} · {r.name}
                      </option>
                    ))}
                </select>
              </Field>
            )}
          </>
        )}
      </div>
    </Dialog>
  );
}

import { useState } from 'react';
import type { Role } from '../types/roles';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { usuariosApi } from '../services/usuariosApi';

export function RoleNameDialog({ role, onClose, onSaved }: { role?: Role; onClose: () => void; onSaved: (r: Role) => void }) {
  const [name, setName] = useState(role?.name ?? '');
  const save = useSave(() => (role ? usuariosApi.updateRole(role.id, { name: name.trim() }) : usuariosApi.createRole({ name: name.trim() })), {
    refetch: [keys.roles, keys.assignableRoles],
    message: role ? 'Rol renombrado.' : 'Rol creado. Ahora elige qué pantallas ve.',
    onDone: onSaved,
  });
  return (
    <Dialog
      title={role ? `Renombrar ${role.name}` : 'Nuevo rol'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Nombre" hint={role ? undefined : 'Empieza sin acceso a ninguna pantalla.'}>
        <input autoFocus maxLength={60} value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>
    </Dialog>
  );
}

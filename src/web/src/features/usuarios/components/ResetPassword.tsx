import { useState } from 'react';
import type { User } from '../types/users';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { usuariosApi } from '../services/usuariosApi';

export function ResetPassword({ user, onClose }: { user: User; onClose: () => void }) {
  const [temporary, setTemporary] = useState('');
  const save = useSave(() => usuariosApi.resetPassword(user.id, { temporaryPassword: temporary }), {
    refetch: [keys.users],
    message: 'Contraseña restablecida. La cambiará al entrar.',
    onDone: onClose,
  });
  return (
    <Dialog
      title={`Restablecer contraseña · ${user.userName}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
      submitText="Restablecer"
    >
      <Field label="Contraseña temporal" hint="Mínimo 8 caracteres. Compártela con la persona; deberá cambiarla al entrar.">
        <input
          autoFocus
          type="text"
          autoComplete="new-password"
          minLength={8}
          value={temporary}
          onChange={(e) => setTemporary(e.target.value)}
          required
        />
      </Field>
    </Dialog>
  );
}

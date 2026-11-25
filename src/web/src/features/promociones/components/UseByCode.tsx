import { useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { useNotify } from '../../../shared/components/Toasts';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { boardKey } from '../services/queryKeys';
import { keys } from '../../../shared/api/queryKeys';
import { promocionesApi } from '../services/promocionesApi';

/** "Marcar como usado" by the beneficio's code (the invitado shows it in the app). */
export function UseByCode() {
  const canUse = useSession().can('promociones', 'edit');
  const notify = useNotify();
  const [code, setCode] = useState('');
  const use = useSave(() => promocionesApi.useBeneficio(code.trim()), {
    refetch: [boardKey, keys.crm],
    onDone: (b) => {
      notify(`${b.name} (${b.code}) quedó usado.`);
      setCode('');
    },
  });
  if (!canUse) return null;
  return (
    <form
      className="actions"
      style={{ margin: '16px 0' }}
      onSubmit={(e) => {
        e.preventDefault();
        if (code.trim()) use.mutate(undefined);
      }}
    >
      <input
        value={code}
        onChange={(e) => setCode(e.target.value)}
        placeholder="Código del beneficio, p. ej. CC-7KQ4-M2XP"
        aria-label="Código del beneficio"
        style={{ minWidth: 320 }}
      />
      <button type="submit" disabled={use.isPending || !code.trim()}>
        Marcar como usado
      </button>
      {use.error && <span className="alert error">{use.error.message}</span>}
    </form>
  );
}

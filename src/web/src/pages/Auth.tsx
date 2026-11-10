import { useState, type FormEvent } from 'react';
import { useSession } from '../session/Session';
import { Field, PageHeader } from '../components/Common';

export function SignIn() {
  const { login } = useSession();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(userName.trim(), password);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="auth">
      <div className="card">
        <div className="card__body">
          <form onSubmit={submit} className="auth-form">
            <div>
              <div className="auth__brand">CleanCom</div>
              <p className="muted">Oficina · inicia sesión para continuar</p>
            </div>
            <Field label="Usuario">
              <input autoFocus autoComplete="username" value={userName} onChange={(e) => setUserName(e.target.value)} required />
            </Field>
            <Field label="Contraseña">
              <input type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </Field>
            {error && <div className="alert error">{error}</div>}
            <button className="primary" disabled={busy}>
              {busy ? 'Entrando…' : 'Entrar'}
            </button>
          </form>
        </div>
      </div>
    </main>
  );
}

/** Required after an initial or temporary password; also reachable from the menu. */
export function ChangePassword({ required }: { required: boolean }) {
  const { user, changePassword, logout } = useSession();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    if (next !== confirmation) {
      setError('La confirmación no coincide con la contraseña nueva.');
      return;
    }
    setBusy(true);
    try {
      await changePassword(current, next);
      setDone(true);
      setCurrent('');
      setNext('');
      setConfirmation('');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  const form = (
    <form onSubmit={submit} className="auth-form">
      {required && (
        <div>
          <div className="auth__brand">CleanCom</div>
          <p className="muted">Hola, {user?.fullName}. Antes de continuar, cambia tu contraseña inicial.</p>
        </div>
      )}
      <Field label="Contraseña actual">
        <input type="password" autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} required />
      </Field>
      <Field label="Contraseña nueva" hint="Mínimo 8 caracteres.">
        <input type="password" autoComplete="new-password" minLength={8} value={next} onChange={(e) => setNext(e.target.value)} required />
      </Field>
      <Field label="Confirma la contraseña nueva">
        <input type="password" autoComplete="new-password" value={confirmation} onChange={(e) => setConfirmation(e.target.value)} required />
      </Field>
      {error && <div className="alert error">{error}</div>}
      {done && !required && <div className="alert info">Contraseña actualizada.</div>}
      <div className="actions">
        <button className="primary" disabled={busy}>
          {busy ? 'Guardando…' : 'Cambiar contraseña'}
        </button>
        {required && (
          <button type="button" className="link" onClick={() => void logout()}>
            Salir
          </button>
        )}
      </div>
    </form>
  );

  if (required) {
    return (
      <main className="auth">
        <div className="card">
          <div className="card__body">{form}</div>
        </div>
      </main>
    );
  }

  return (
    <>
      <PageHeader title="Mi contraseña" description="Mínimo 8 caracteres." />
      <div className="card" style={{ maxWidth: 420 }}>
        <div className="card__body">{form}</div>
      </div>
    </>
  );
}

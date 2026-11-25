import { type FormEvent, useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { Logo } from '../../../shared/components/Brand';
import { Field } from '../../../shared/components/Form';

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
              <Logo className="auth__logo" />
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

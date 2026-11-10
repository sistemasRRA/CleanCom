import { useState } from 'react';
import { NavLink, Outlet } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useOperadores, useRutas } from '../../api/queries';
import { ActiveBadge, Checkbox, Dialog, Field, PageHeader, QueryState, formatDateTime, optional, useSave } from '../../components/Common';
import { useSession } from '../../session/Session';

type User = Schemas['UserDto'];
type Operador = Schemas['OperadorDto'];

/** The office only creates Administracion and Chofer users; invitados sign up from their app. */
const ROLES = [
  { key: 'Administracion', name: 'Administración' },
  { key: 'Chofer', name: 'Chofer' },
];
const roleName = (role: string) => ROLES.find((r) => r.key === role)?.name ?? role;

export function Users() {
  return (
    <>
      <PageHeader title="Usuarios" description="Usuarios de oficina y choferes, y operadores de la nómina." />
      <nav className="tabs">
        <NavLink to="lista">Usuarios</NavLink>
        <NavLink to="operadores">Operadores</NavLink>
      </nav>
      <Outlet />
    </>
  );
}

// ---------------- Users ----------------

export function UserList() {
  const canEdit = useSession().can('usuarios', 'edit');
  const [role, setRole] = useState('');
  const users = useQuery({
    queryKey: [...keys.users, role],
    queryFn: () => unwrap(api.GET('/usuarios', { params: { query: { role: role || undefined } } })),
  });
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<User | null>(null);
  const [resetting, setResetting] = useState<User | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <h2>Usuarios</h2>
        <div className="actions">
          <select value={role} onChange={(e) => setRole(e.target.value)} aria-label="Rol">
            <option value="">Todos los roles</option>
            {ROLES.map((r) => (
              <option key={r.key} value={r.key}>
                {r.name}
              </option>
            ))}
          </select>
          {canEdit && (
            <button type="button" className="primary" onClick={() => setCreating(true)}>
              Nuevo usuario
            </button>
          )}
        </div>
      </div>
      <QueryState loading={users.isPending} error={users.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Persona</th>
                <th>Rol</th>
                <th>Chofer vinculado</th>
                <th>Último acceso</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.data?.map((u) => (
                <tr key={u.id} className={u.active ? '' : 'inactive'}>
                  <td>
                    <strong>{u.fullName}</strong>
                    <div className="muted">
                      <code>{u.userName}</code>
                      {u.phone && ` · ${u.phone}`}
                    </div>
                  </td>
                  <td>{roleName(u.role)}</td>
                  <td>{u.operadorName ?? <span className="muted">—</span>}</td>
                  <td className="muted">{formatDateTime(u.lastAccess)}</td>
                  <td>
                    <div className="actions tight">
                      <ActiveBadge active={u.active} />
                      {u.mustChangePassword && <span className="badge warning">Debe cambiar contraseña</span>}
                    </div>
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        <button type="button" className="small" onClick={() => setEditing(u)}>
                          Editar
                        </button>
                        <button type="button" className="small" onClick={() => setResetting(u)}>
                          Restablecer contraseña
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {users.data?.length === 0 && <div className="empty">Sin usuarios.</div>}
        </div>
      </QueryState>
      {creating && <NewUser onClose={() => setCreating(false)} />}
      {editing && <EditUser user={editing} onClose={() => setEditing(null)} />}
      {resetting && <ResetPassword user={resetting} onClose={() => setResetting(null)} />}
    </section>
  );
}

function NewUser({ onClose }: { onClose: () => void }) {
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
      unwrap(
        api.POST('/usuarios', {
          body: {
            fullName: f.fullName.trim(),
            userName: f.userName.trim(),
            email: optional(f.email),
            phone: optional(f.phone),
            role: f.role,
            initialPassword: f.initialPassword,
            operadorId: isChofer && f.operador !== 'new' && f.operador !== 'none' ? Number(f.operador) : null,
            createOperador: isChofer && f.operador === 'new',
            usualRutaId: isChofer && f.operador === 'new' && f.usualRutaId ? Number(f.usualRutaId) : null,
          },
        }),
      ),
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
            {ROLES.map((r) => (
              <option key={r.key} value={r.key}>
                {r.name}
              </option>
            ))}
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

function EditUser({ user, onClose }: { user: User; onClose: () => void }) {
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
      unwrap(
        api.PUT('/usuarios/{id}', {
          params: { path: { id: user.id } },
          body: {
            fullName: f.fullName.trim(),
            email: optional(f.email),
            phone: optional(f.phone),
            role: f.role,
            active: f.active,
            operadorId: f.role === 'Chofer' && f.operadorId ? Number(f.operadorId) : null,
          },
        }),
      ),
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
            {ROLES.map((r) => (
              <option key={r.key} value={r.key}>
                {r.name}
              </option>
            ))}
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

function ResetPassword({ user, onClose }: { user: User; onClose: () => void }) {
  const [temporary, setTemporary] = useState('');
  const save = useSave(
    () =>
      unwrap(
        api.POST('/usuarios/{id}/restablecer', {
          params: { path: { id: user.id } },
          body: { temporaryPassword: temporary },
        }),
      ),
    { refetch: [keys.users], message: 'Contraseña restablecida. La cambiará al entrar.', onDone: onClose },
  );
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

// ---------------- Operadores ----------------

export function Operadores() {
  const canEdit = useSession().can('usuarios', 'edit');
  const operadores = useOperadores();
  const rutas = useRutas();
  const [editing, setEditing] = useState<Operador | 'new' | null>(null);
  const findRuta = (id?: number | null) => rutas.data?.find((r) => r.id === id);

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Operadores</h2>
          <p className="muted">Choferes de la nómina. Pueden existir sin usuario en la app.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nuevo operador
          </button>
        )}
      </div>
      <QueryState loading={operadores.isPending} error={operadores.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Teléfono</th>
                <th>Ruta habitual</th>
                <th>Usuario</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {operadores.data?.map((o) => {
                const ruta = findRuta(o.usualRutaId);
                return (
                  <tr key={o.id} className={o.active ? '' : 'inactive'}>
                    <td>
                      <strong>{o.name}</strong>
                    </td>
                    <td>{o.phone ?? <span className="muted">—</span>}</td>
                    <td>
                      {ruta ? (
                        <>
                          <span className="ruta-color" style={{ background: ruta.color }} />
                          {ruta.code}
                        </>
                      ) : (
                        <span className="muted">—</span>
                      )}
                    </td>
                    <td>{o.userId ? <span className="badge brand">con usuario</span> : <span className="muted">sin usuario</span>}</td>
                    <td>
                      <ActiveBadge active={o.active} />
                    </td>
                    <td className="end">
                      {canEdit && (
                        <button type="button" className="small" onClick={() => setEditing(o)}>
                          Editar
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {operadores.data?.length === 0 && <div className="empty">Sin operadores.</div>}
        </div>
      </QueryState>
      {editing && <OperadorForm operador={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function OperadorForm({ operador, onClose }: { operador: Operador | null; onClose: () => void }) {
  const rutas = useRutas();
  const [name, setName] = useState(operador?.name ?? '');
  const [phone, setPhone] = useState(operador?.phone ?? '');
  const [rutaId, setRutaId] = useState(operador?.usualRutaId?.toString() ?? '');
  const [active, setActive] = useState(operador?.active ?? true);
  const save = useSave(
    () => {
      const body = { name: name.trim(), phone: optional(phone), usualRutaId: rutaId ? Number(rutaId) : null, active };
      return operador
        ? unwrap(api.PUT('/operadores/{id}', { params: { path: { id: operador.id } }, body }))
        : unwrap(api.POST('/operadores', { body }));
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

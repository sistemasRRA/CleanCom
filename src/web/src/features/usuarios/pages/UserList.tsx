import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import type { User } from '../types/users';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { RoleOptions } from '../components/RoleOptions';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { formatDateTime } from '../../../shared/utils/dates';
import { NewUser } from '../components/NewUser';
import { EditUser } from '../components/EditUser';
import { ResetPassword } from '../components/ResetPassword';
import { usuariosApi } from '../services/usuariosApi';

// ---------------- Users ----------------

export function UserList() {
  const canEdit = useSession().can('usuarios', 'edit');
  const [role, setRole] = useState('');
  const users = useQuery({
    queryKey: [...keys.users, role],
    queryFn: () => usuariosApi.list({ role: role || undefined }),
  });
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<User | null>(null);
  const [resetting, setResetting] = useState<User | null>(null);

  const pagedUsers = usePaged(users.data);
  return (
    <section className="card">
      <div className="card__header">
        <h2>Usuarios</h2>
        <div className="actions">
          <select value={role} onChange={(e) => setRole(e.target.value)} aria-label="Rol">
            <option value="">Todos los roles</option>
            <RoleOptions />
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
              {pagedUsers.rows.map((u) => (
                <tr key={u.id} className={u.active ? '' : 'inactive'}>
                  <td>
                    <strong>{u.fullName}</strong>
                    <div className="muted">
                      <code>{u.userName}</code>
                      {u.phone && ` · ${u.phone}`}
                    </div>
                  </td>
                  <td>{u.roleName}</td>
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
        <Pagination {...pagedUsers} />
      </QueryState>
      {creating && <NewUser onClose={() => setCreating(false)} />}
      {editing && <EditUser user={editing} onClose={() => setEditing(null)} />}
      {resetting && <ResetPassword user={resetting} onClose={() => setResetting(null)} />}
    </section>
  );
}

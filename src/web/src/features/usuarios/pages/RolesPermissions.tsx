import { useSearchParams } from 'react-router';
import { useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { useRoles, useScreens } from '../services/queries';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { RoleEditor } from '../components/RoleEditor';
import { RoleNameDialog } from '../components/RoleNameDialog';

export function RolesPermissions() {
  const canEdit = useSession().can('roles-y-permisos', 'edit');
  const roles = useRoles();
  const screens = useScreens();
  // The selected role lives in the URL (?rol=5) so a link opens it directly.
  const [params, setParams] = useSearchParams();
  const selectedId = Number(params.get('rol')) || null;
  const setSelectedId = (id: number | null) => setParams(id ? { rol: String(id) } : {}, { replace: true });
  const [creating, setCreating] = useState(false);

  // Invitado only uses its app; it is managed from there.
  const visible = roles.data?.filter((r) => r.assignableInOffice) ?? [];
  const selected = visible.find((r) => r.id === selectedId) ?? visible[0];

  return (
    <>
      <PageHeader
        title="Roles y permisos"
        description="Qué pantallas del menú ve y edita cada rol. Los cambios aplican sin volver a iniciar sesión."
      />
      <QueryState loading={roles.isPending || screens.isPending} error={roles.error ?? screens.error}>
        <div className="roles">
          <section className="card roles__list">
            <div className="card__header">
              <h2>Roles</h2>
              {canEdit && (
                <button type="button" className="small primary" onClick={() => setCreating(true)}>
                  Nuevo rol
                </button>
              )}
            </div>
            <ul>
              {visible.map((r) => (
                <li key={r.id}>
                  <button
                    type="button"
                    className={r.id === selected?.id ? 'active' : undefined}
                    onClick={() => setSelectedId(r.id)}
                    aria-current={r.id === selected?.id}
                  >
                    <span>{r.name}</span>
                    <span className="muted">
                      {r.userCount} {r.userCount === 1 ? 'usuario' : 'usuarios'}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </section>
          {selected && screens.data && (
            <RoleEditor key={selected.id} role={selected} screens={screens.data} canEdit={canEdit} onDeleted={() => setSelectedId(null)} />
          )}
        </div>
        {creating && (
          <RoleNameDialog
            onClose={() => setCreating(false)}
            onSaved={(r) => {
              setSelectedId(r.id);
              setCreating(false);
            }}
          />
        )}
      </QueryState>
    </>
  );
}

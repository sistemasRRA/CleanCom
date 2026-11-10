import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useRoles, useScreens } from '../../api/queries';
import { Dialog, Field, PageHeader, QueryState, useSave } from '../../components/Common';
import { SCREENS } from '../../screens';
import { useSession } from '../../session/Session';

type Role = Schemas['RoleDto'];
type ScreenInfo = Schemas['ScreenDto'];
type Level = Schemas['AccessLevel'];
/** Level chosen for each screen; a missing screen means no access. */
type Draft = Record<string, Level>;

const OPTIONS: { value: Level | 'none'; label: string }[] = [
  { value: 'none', label: 'Sin acceso' },
  { value: 'View', label: 'Ver' },
  { value: 'Edit', label: 'Editar' },
];

/** Same order as the side menu; the apps go last. */
function menuOrder(screens: ScreenInfo[]) {
  const position = (key: string) => {
    const i = SCREENS.findIndex((s) => s.key === key);
    return i === -1 ? SCREENS.length : i;
  };
  const sorted = [...screens].sort((a, b) => position(a.key) - position(b.key));
  const groups: { name: string; screens: ScreenInfo[] }[] = [];
  for (const s of sorted) {
    const group = groups.find((g) => g.name === s.group);
    if (group) group.screens.push(s);
    else groups.push({ name: s.group, screens: [s] });
  }
  return groups;
}

const toDraft = (role: Role): Draft => Object.fromEntries(role.permissions.map((p) => [p.screen, p.level]));

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
      <PageHeader title="Roles y permisos" description="Qué pantallas del menú ve y edita cada rol. Los cambios aplican sin volver a iniciar sesión." />
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

function RoleEditor({ role, screens, canEdit, onDeleted }: { role: Role; screens: ScreenInfo[]; canEdit: boolean; onDeleted: () => void }) {
  const queryClient = useQueryClient();
  const groups = useMemo(() => menuOrder(screens), [screens]);
  const [draft, setDraft] = useState<Draft>(() => toDraft(role));
  const [renaming, setRenaming] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const editable = canEdit && !role.lockedPermissions;

  const original = toDraft(role);
  const dirty =
    Object.keys({ ...original, ...draft }).some((k) => original[k] !== draft[k]);

  const set = (screen: string, value: Level | 'none') =>
    setDraft((d) => {
      const next = { ...d };
      if (value === 'none') delete next[screen];
      else next[screen] = value;
      return next;
    });

  const setGroup = (group: ScreenInfo[], value: Level | 'none') => group.forEach((s) => set(s.key, value));

  const save = useSave(
    () =>
      unwrap(
        api.PUT('/roles/{id}/permisos', {
          params: { path: { id: role.id } },
          body: { permissions: Object.entries(draft).map(([screen, level]) => ({ screen, level })) },
        }),
      ),
    {
      refetch: [keys.roles],
      message: `Permisos de ${role.name} guardados. Ya aplican a sus usuarios.`,
      // If it is the user's own role, the side menu changes right away.
      onDone: () => void queryClient.invalidateQueries({ queryKey: keys.me }),
    },
  );

  const remove = useSave(() => unwrap(api.DELETE('/roles/{id}', { params: { path: { id: role.id } } })), {
    refetch: [keys.roles],
    message: `Rol ${role.name} borrado.`,
    onDone: onDeleted,
  });

  const menu = groups
    .filter((g) => g.name !== 'Apps')
    .map((g) => ({ name: g.name, screens: g.screens.filter((s) => draft[s.key]) }))
    .filter((g) => g.screens.length > 0);
  const apps = screens.filter((s) => s.group === 'Apps' && draft[s.key]);

  return (
    <>
      <section className="card roles__editor">
        <div className="card__header">
          <div>
            <h2>{role.name}</h2>
            <p className="muted">
              {role.userCount} {role.userCount === 1 ? 'usuario' : 'usuarios'}
              {role.system ? ' · rol del sistema' : ' · creado por la oficina'}
            </p>
          </div>
          {canEdit && !role.system && (
            <div className="actions">
              <button type="button" className="small" onClick={() => setRenaming(true)}>
                Renombrar
              </button>
              <button type="button" className="small danger" onClick={() => setConfirmDelete(true)}>
                Borrar
              </button>
            </div>
          )}
        </div>

        {role.lockedPermissions && (
          <div className="card__body">
            <div className="alert info">
              SuperUser siempre ve y edita todas las pantallas de oficina, para que el sistema nunca se quede sin quien lo
              administre.
            </div>
          </div>
        )}

        <div className="table-wrap">
          <table className="permissions">
            <tbody>
              {groups.map((g) => (
                <PermissionGroup key={g.name} name={g.name} screens={g.screens} draft={draft} editable={editable} onSet={set} onSetGroup={setGroup} />
              ))}
            </tbody>
          </table>
        </div>

        {editable && (
          <div className="roles__footer">
            <span className="muted">{dirty ? 'Hay cambios sin guardar.' : 'Sin cambios.'}</span>
            <div className="actions">
              <button type="button" disabled={!dirty || save.isPending} onClick={() => setDraft(toDraft(role))}>
                Descartar
              </button>
              <button type="button" className="primary" disabled={!dirty || save.isPending} onClick={() => save.mutate(undefined)}>
                {save.isPending ? 'Guardando…' : 'Guardar permisos'}
              </button>
            </div>
          </div>
        )}
        {save.error && (
          <div className="card__body">
            <div className="alert error">{save.error.message}</div>
          </div>
        )}
      </section>

      <aside className="card roles__preview" aria-label="Vista previa del menú">
        <div className="card__header">
          <h3>Así se ve su menú</h3>
        </div>
        <div className="roles__preview-menu">
          <div className="roles__preview-item">Inicio</div>
          {menu.map((g) => (
            <div key={g.name}>
              <div className="roles__preview-group">{g.name}</div>
              {g.screens.map((s) => (
                <div key={s.key} className="roles__preview-item">
                  {s.name}
                  {draft[s.key] === 'View' && <span>solo ver</span>}
                </div>
              ))}
            </div>
          ))}
          {menu.length === 0 && <p className="roles__preview-empty">Sin pantallas de oficina.</p>}
        </div>
        {apps.length > 0 && (
          <div className="card__body muted">Además entra a: {apps.map((a) => a.name).join(', ')}.</div>
        )}
      </aside>

      {renaming && <RoleNameDialog role={role} onClose={() => setRenaming(false)} onSaved={() => setRenaming(false)} />}
      {confirmDelete && (
        <Dialog
          title={`Borrar el rol ${role.name}`}
          onClose={() => setConfirmDelete(false)}
          onSubmit={() => remove.mutate(undefined)}
          busy={remove.isPending}
          error={remove.error}
          submitText="Borrar"
        >
          <p>
            {role.userCount > 0
              ? `Tiene ${role.userCount} usuario(s). Cámbialos de rol antes de borrarlo.`
              : 'Se borran el rol y sus permisos. No se puede deshacer.'}
          </p>
        </Dialog>
      )}
    </>
  );
}

function PermissionGroup({
  name,
  screens,
  draft,
  editable,
  onSet,
  onSetGroup,
}: {
  name: string;
  screens: ScreenInfo[];
  draft: Draft;
  editable: boolean;
  onSet: (screen: string, value: Level | 'none') => void;
  onSetGroup: (screens: ScreenInfo[], value: Level | 'none') => void;
}) {
  return (
    <>
      <tr className="permissions__group">
        <th>{name}</th>
        <th className="end">
          {editable && (
            <span className="permissions__bulk">
              Todo:
              {OPTIONS.map((o) => (
                <button key={o.value} type="button" className="link" onClick={() => onSetGroup(screens, o.value)}>
                  {o.label}
                </button>
              ))}
            </span>
          )}
        </th>
      </tr>
      {screens.map((s) => {
        const value = draft[s.key] ?? 'none';
        const week = SCREENS.find((m) => m.key === s.key)?.week;
        return (
          <tr key={s.key}>
            <td>
              {s.name}
              {week && <span className="muted"> · sem. {week}</span>}
            </td>
            <td className="end">
              <div className="segmented" role="radiogroup" aria-label={`Acceso a ${s.name}`}>
                {OPTIONS.map((o) => (
                  <button
                    key={o.value}
                    type="button"
                    role="radio"
                    aria-checked={value === o.value}
                    className={`segmented__option is-${o.value.toLowerCase()}${value === o.value ? ' selected' : ''}`}
                    disabled={!editable}
                    onClick={() => onSet(s.key, o.value)}
                  >
                    {o.label}
                  </button>
                ))}
              </div>
            </td>
          </tr>
        );
      })}
    </>
  );
}

function RoleNameDialog({ role, onClose, onSaved }: { role?: Role; onClose: () => void; onSaved: (r: Role) => void }) {
  const [name, setName] = useState(role?.name ?? '');
  const save = useSave(
    () =>
      role
        ? unwrap(api.PUT('/roles/{id}', { params: { path: { id: role.id } }, body: { name: name.trim() } }))
        : unwrap(api.POST('/roles', { body: { name: name.trim() } })),
    {
      refetch: [keys.roles, keys.assignableRoles],
      message: role ? 'Rol renombrado.' : 'Rol creado. Ahora elige qué pantallas ve.',
      onDone: onSaved,
    },
  );
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

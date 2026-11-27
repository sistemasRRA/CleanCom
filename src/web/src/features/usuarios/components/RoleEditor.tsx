import { useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { type Draft, type Level, menuOrder, type Role, type ScreenInfo, SPECIAL_GROUP, toDraft } from '../types/roles';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { PermissionGroup } from './PermissionGroup';
import { RoleNameDialog } from './RoleNameDialog';
import { Dialog } from '../../../shared/components/Dialog';
import { usuariosApi } from '../services/usuariosApi';

export function RoleEditor({ role, screens, canEdit, onDeleted }: { role: Role; screens: ScreenInfo[]; canEdit: boolean; onDeleted: () => void }) {
  const queryClient = useQueryClient();
  const groups = useMemo(() => menuOrder(screens), [screens]);
  const [draft, setDraft] = useState<Draft>(() => toDraft(role));
  const [renaming, setRenaming] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const editable = canEdit && !role.lockedPermissions;

  const original = toDraft(role);
  const dirty = Object.keys({ ...original, ...draft }).some((k) => original[k] !== draft[k]);

  const set = (screen: string, value: Level | 'none') =>
    setDraft((d) => {
      const next = { ...d };
      if (value === 'none') delete next[screen];
      else next[screen] = value;
      return next;
    });

  const setGroup = (group: ScreenInfo[], value: Level | 'none') => group.forEach((s) => set(s.key, value));

  const save = useSave(
    () => usuariosApi.updatePermissions(role.id, { permissions: Object.entries(draft).map(([screen, level]) => ({ screen, level })) }),
    {
      refetch: [keys.roles],
      message: `Permisos de ${role.name} guardados. Ya aplican a sus usuarios.`,
      // If it is the user's own role, the side menu changes right away.
      onDone: () => void queryClient.invalidateQueries({ queryKey: keys.me }),
    },
  );

  const remove = useSave(() => usuariosApi.removeRole(role.id), {
    refetch: [keys.roles],
    message: `Rol ${role.name} borrado.`,
    onDone: onDeleted,
  });

  const menu = groups
    .filter((g) => g.name !== 'Apps' && g.name !== SPECIAL_GROUP)
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
              SuperUser siempre ve y edita todas las pantallas de oficina, para que el sistema nunca se quede sin quien lo administre.
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
        {apps.length > 0 && <div className="card__body muted">Además entra a: {apps.map((a) => a.name).join(', ')}.</div>}
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

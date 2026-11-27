import { type Draft, type Level, OPTIONS, type ScreenInfo, SPECIAL_GROUP, SPECIAL_OPTIONS } from '../types/roles';
import { SCREENS } from '../../../app/screens';

export function PermissionGroup({
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
  const options = name === SPECIAL_GROUP ? SPECIAL_OPTIONS : OPTIONS;
  return (
    <>
      <tr className="permissions__group">
        <th>{name}</th>
        <th className="end">
          {editable && (
            <span className="permissions__bulk">
              Todo:
              {options.map((o) => (
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
                {options.map((o) => (
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

import type { Schemas } from '../../../shared/api/client';
import { SCREENS } from '../../../app/screens';

export type Role = Schemas['RoleDto'];

export type ScreenInfo = Schemas['ScreenDto'];

export type Level = Schemas['AccessLevel'];

/** Level chosen for each screen; a missing screen means no access. */
export type Draft = Record<string, Level>;

/** Actions with their own permission (not screens): only "Permitido" (Edit) counts. */
export const SPECIAL_GROUP = 'Permisos especiales';

export const SPECIAL_OPTIONS: { value: Level | 'none'; label: string }[] = [
  { value: 'none', label: 'Sin acceso' },
  { value: 'Edit', label: 'Permitido' },
];

export const OPTIONS: { value: Level | 'none'; label: string }[] = [
  { value: 'none', label: 'Sin acceso' },
  { value: 'View', label: 'Ver' },
  { value: 'Edit', label: 'Editar' },
];

/** Same order as the side menu; the apps go last. */
export function menuOrder(screens: ScreenInfo[]) {
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

export const toDraft = (role: Role): Draft => Object.fromEntries(role.permissions.map((p) => [p.screen, p.level]));

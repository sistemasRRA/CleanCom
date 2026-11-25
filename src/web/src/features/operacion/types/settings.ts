import type { Schemas } from '../../../shared/api/client';

export type Setting = Schemas['SettingDto'];

export function displayValue(type: Setting['type'], value: string) {
  if (type === 'Boolean') return value === 'true' ? 'Sí' : 'No';
  return value;
}

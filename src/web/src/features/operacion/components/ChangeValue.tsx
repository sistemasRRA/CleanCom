import { useState } from 'react';
import type { Setting } from '../types/settings';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { optional } from '../../../shared/utils/format';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

const TYPE_HINTS: Record<Setting['type'], string> = {
  Text: 'Texto libre.',
  Integer: 'Número entero.',
  Decimal: 'Número con decimales, con punto.',
  Boolean: 'Sí o no.',
  Time: 'Hora en formato 24 h, por ejemplo 13:00.',
  TimeRange: 'Dos horas separadas por guion, por ejemplo 09:00-13:00.',
  Coordinate: 'Latitud y longitud separadas por coma, por ejemplo 19.4326,-99.1332.',
};

export function ChangeValue({ setting, onClose }: { setting: Setting; onClose: () => void }) {
  const [value, setValue] = useState(setting.value);
  const [schedule, setSchedule] = useState(false);
  const [from, setFrom] = useState('');
  const [note, setNote] = useState('');
  const save = useSave(
    () =>
      operacionApi.updateSetting({
        key: setting.key,
        value,
        effectiveFrom: schedule && from ? mexicoCityToIso(from) : null,
        note: optional(note),
      }),
    { refetch: [keys.settings, keys.settingHistory(setting.key)], message: 'Regla actualizada.', onDone: onClose },
  );

  return (
    <Dialog title={setting.description} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <Field label="Valor" hint={TYPE_HINTS[setting.type]}>
        {setting.type === 'Boolean' ? (
          <select value={value} onChange={(e) => setValue(e.target.value)}>
            <option value="true">Sí</option>
            <option value="false">No</option>
          </select>
        ) : (
          <input autoFocus value={value} onChange={(e) => setValue(e.target.value)} required />
        )}
      </Field>
      <Checkbox checked={schedule} onChange={setSchedule}>
        Programar a partir de una fecha
      </Checkbox>
      {schedule && (
        <Field label="Vigente desde (hora de Ciudad de México)">
          <input type="datetime-local" value={from} onChange={(e) => setFrom(e.target.value)} required />
        </Field>
      )}
      <Field label="Nota" hint="Opcional. Queda en el historial.">
        <input value={note} onChange={(e) => setNote(e.target.value)} />
      </Field>
    </Dialog>
  );
}

/**
 * Turns "2026-11-20T13:00" typed in Mexico City time into ISO with its offset,
 * without assuming the offset: Intl provides it for that date.
 */
function mexicoCityToIso(local: string) {
  const asUtc = new Date(`${local}:00Z`);
  const zoneName = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Mexico_City', timeZoneName: 'longOffset' })
    .formatToParts(asUtc)
    .find((p) => p.type === 'timeZoneName')?.value; // "GMT-06:00"
  const offset = zoneName?.replace('GMT', '') || '+00:00';
  return `${local}:00${offset}`;
}

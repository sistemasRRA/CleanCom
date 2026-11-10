import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys } from '../../api/queries';
import { Checkbox, Dialog, Field, QueryState, formatDateTime, optional, useSave } from '../../components/Common';
import { useSession } from '../../session/Session';

type Setting = Schemas['SettingDto'];

const TYPE_HINTS: Record<Setting['type'], string> = {
  Text: 'Texto libre.',
  Integer: 'Número entero.',
  Decimal: 'Número con decimales, con punto.',
  Boolean: 'Sí o no.',
  Time: 'Hora en formato 24 h, por ejemplo 13:00.',
  TimeRange: 'Dos horas separadas por guion, por ejemplo 09:00-13:00.',
  Coordinate: 'Latitud y longitud separadas por coma, por ejemplo 19.4326,-99.1332.',
};

export function Settings() {
  const canEdit = useSession().can('operacion', 'edit');
  const query = useQuery({ queryKey: keys.settings, queryFn: () => unwrap(api.GET('/configuracion')) });
  const [changing, setChanging] = useState<Setting | null>(null);
  const [history, setHistory] = useState<string | null>(null);

  // Grouped by the key prefix: "Agenda.CorteMismoDia" → "Agenda".
  const groups = new Map<string, Setting[]>();
  for (const s of query.data ?? []) {
    const group = s.key.split('.')[0];
    groups.set(group, [...(groups.get(group) ?? []), s]);
  }

  return (
    <QueryState loading={query.isPending} error={query.error}>
      <div className="stack">
        {[...groups].map(([group, list]) => (
          <section key={group} className="card">
            <div className="card__header">
              <h2>{group}</h2>
            </div>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Regla</th>
                    <th>Valor</th>
                    <th>Vigente desde</th>
                    <th>Programado</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {list.map((s) => (
                    <tr key={s.key}>
                      <td>
                        <div>{s.description}</div>
                        <code className="muted">{s.key}</code>
                        {s.perRuta && (
                          <span className="badge brand" style={{ marginLeft: 6 }}>
                            se ajusta por ruta
                          </span>
                        )}
                      </td>
                      <td>
                        <strong>{displayValue(s.type, s.value)}</strong>
                      </td>
                      <td className="muted">{formatDateTime(s.effectiveFrom)}</td>
                      <td>
                        {s.scheduled ? (
                          <span className="badge warning">
                            {displayValue(s.type, s.scheduled.value)} desde {formatDateTime(s.scheduled.effectiveFrom)}
                          </span>
                        ) : (
                          <span className="muted">—</span>
                        )}
                      </td>
                      <td>
                        <div className="actions end">
                          <button type="button" className="small" onClick={() => setHistory(s.key)}>
                            Historial
                          </button>
                          {canEdit && (
                            <button type="button" className="small" onClick={() => setChanging(s)}>
                              Cambiar
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        ))}
      </div>
      {changing && <ChangeValue setting={changing} onClose={() => setChanging(null)} />}
      {history && <History settingKey={history} onClose={() => setHistory(null)} />}
    </QueryState>
  );
}

function displayValue(type: Setting['type'], value: string) {
  if (type === 'Boolean') return value === 'true' ? 'Sí' : 'No';
  return value;
}

function ChangeValue({ setting, onClose }: { setting: Setting; onClose: () => void }) {
  const [value, setValue] = useState(setting.value);
  const [schedule, setSchedule] = useState(false);
  const [from, setFrom] = useState('');
  const [note, setNote] = useState('');
  const save = useSave(
    () =>
      unwrap(
        api.PUT('/configuracion', {
          body: {
            key: setting.key,
            value,
            effectiveFrom: schedule && from ? mexicoCityToIso(from) : null,
            note: optional(note),
          },
        }),
      ),
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

function History({ settingKey, onClose }: { settingKey: string; onClose: () => void }) {
  const query = useQuery({
    queryKey: keys.settingHistory(settingKey),
    queryFn: () => unwrap(api.GET('/configuracion/historial', { params: { query: { key: settingKey } } })),
  });
  return (
    <Dialog title={`Historial · ${settingKey}`} onClose={onClose} onSubmit={onClose} submitText="Cerrar">
      <QueryState loading={query.isPending} error={query.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Valor</th>
                <th>Vigente desde</th>
                <th>Registrado</th>
                <th>Nota</th>
              </tr>
            </thead>
            <tbody>
              {query.data?.map((h) => (
                <tr key={h.id}>
                  <td>
                    <strong>{h.value}</strong>
                  </td>
                  <td>{formatDateTime(h.effectiveFrom)}</td>
                  <td className="muted">{formatDateTime(h.createdAt)}</td>
                  <td className="muted">{h.note ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
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

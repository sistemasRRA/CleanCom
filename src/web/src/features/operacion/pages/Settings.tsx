import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { displayValue, type Setting } from '../types/settings';
import { QueryState } from '../../../shared/components/Page';
import { formatDateTime } from '../../../shared/utils/dates';
import { ChangeValue } from '../components/ChangeValue';
import { History } from '../components/SettingHistory';
import { operacionApi } from '../services/operacionApi';

export function Settings() {
  const canEdit = useSession().can('operacion', 'edit');
  const query = useQuery({ queryKey: keys.settings, queryFn: () => operacionApi.listSettings() });
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

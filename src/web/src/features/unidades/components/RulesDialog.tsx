import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { listKey, rulesKey } from '../services/queryKeys';
import { type Schemas } from '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { Dialog } from '../../../shared/components/Dialog';
import { QueryState } from '../../../shared/components/Page';
import { ALERT_EVENT } from '../types/unidades';
import { unidadesApi } from '../services/unidadesApi';

/** Days, recipients and channels of each fleet notice (GET|PUT /avisos-flota/reglas). */
export function RulesDialog({ canEdit, onClose }: { canEdit: boolean; onClose: () => void }) {
  const rules = useQuery({ queryKey: rulesKey, queryFn: () => unidadesApi.alertRules() });
  const [edited, setEdited] = useState<Schemas['FleetAlertRuleDto'][] | null>(null);
  const rows = edited ?? rules.data ?? [];
  const set = (i: number, patch: Partial<Schemas['FleetAlertRuleDto']>) => setEdited(rows.map((r, j) => (j === i ? { ...r, ...patch } : r)));
  const save = useSave(() => unidadesApi.saveAlertRules(rows), {
    refetch: [rulesKey, listKey],
    message: 'Reglas de aviso guardadas.',
    onDone: onClose,
  });
  return (
    <Dialog
      title="Reglas de aviso de flota"
      onClose={onClose}
      onSubmit={() => (canEdit ? save.mutate(undefined) : onClose())}
      busy={save.isPending}
      error={save.error}
      submitText={canEdit ? 'Guardar' : 'Cerrar'}
    >
      <QueryState loading={rules.isLoading} error={rules.error} inline>
        <p className="muted">
          Días antes de la fecha en que sale el aviso, separados por coma (0 es el mismo día). Después de la fecha se repite cada 7 días hasta que se
          registre. Los avisos por correo y en la web llegan con las notificaciones (semana 6).
        </p>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Evento</th>
                <th>Días antes</th>
                <th>Destinatarios</th>
                <th>Canales</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={r.event}>
                  <td>{ALERT_EVENT[r.event]}</td>
                  <td>
                    <input
                      value={r.daysBefore}
                      disabled={!canEdit}
                      onChange={(e) => set(i, { daysBefore: e.target.value })}
                      aria-label={`Días antes · ${ALERT_EVENT[r.event]}`}
                      size={8}
                    />
                  </td>
                  <td>
                    <input
                      value={r.recipients}
                      disabled={!canEdit}
                      onChange={(e) => set(i, { recipients: e.target.value })}
                      aria-label={`Destinatarios · ${ALERT_EVENT[r.event]}`}
                      placeholder="Correos o roles"
                    />
                  </td>
                  <td>
                    <input
                      value={r.channels}
                      disabled={!canEdit}
                      onChange={(e) => set(i, { channels: e.target.value })}
                      aria-label={`Canales · ${ALERT_EVENT[r.event]}`}
                      placeholder="Correo,Web,Push"
                      size={14}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
    </Dialog>
  );
}

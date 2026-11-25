import { useQuery } from '@tanstack/react-query';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { Dialog } from '../../../shared/components/Dialog';
import { QueryState } from '../../../shared/components/Page';
import { formatDateTime } from '../../../shared/utils/dates';
import { operacionApi } from '../services/operacionApi';

export function History({ settingKey, onClose }: { settingKey: string; onClose: () => void }) {
  const query = useQuery({
    queryKey: keys.settingHistory(settingKey),
    queryFn: () => operacionApi.settingHistory({ key: settingKey }),
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

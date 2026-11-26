import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { Dialog } from '../../../shared/components/Dialog';
import { QueryState } from '../../../shared/components/Page';
import { useSession } from '../../../shared/session/Session';
import { shortDay } from '../../../shared/utils/dates';
import { kpisApi } from '../services/kpisApi';
import { kpisKey } from '../services/queryKeys';
import { formatValue, type KpiBoard, type KpiCard, type KpiPeriod } from '../types/kpis';

/** What forms an indicator: each visit, ticket, pedido or invitado counted, optionally of one ruta. */
export function KpiDetailDialog({
  card,
  periodo,
  fecha,
  label,
  rutas,
  onClose,
}: {
  card: KpiCard;
  periodo: KpiPeriod;
  fecha: string;
  label: string;
  rutas: KpiBoard['rutas'];
  onClose: () => void;
}) {
  const canSeePedidos = useSession().can('pedidos');
  const [rutaId, setRutaId] = useState<number | undefined>();
  const detail = useQuery({
    queryKey: [...kpisKey, 'detalle', card.key, periodo, fecha, rutaId],
    queryFn: () => kpisApi.detail(card.key, { periodo, fecha, rutaId }),
  });
  const d = detail.data;
  const showDenominator = d?.rows.some((r) => r.denominator !== 0);

  return (
    <Dialog title={card.name} onClose={onClose} onSubmit={onClose} submitText="Cerrar" hideCancel className="kpi-detail">
      <p className="muted" style={{ marginTop: 0 }}>
        {label} · {card.calculation}
      </p>
      <div className="kpi-detail__bar">
        <strong className="kpi-detail__value">{formatValue(card.unit, d ? d.value : card.value)}</strong>
        {rutas.length > 1 && (
          <select value={rutaId ?? ''} onChange={(e) => setRutaId(e.target.value ? Number(e.target.value) : undefined)} aria-label="Ruta">
            <option value="">Todas las rutas</option>
            {rutas.map((r) => (
              <option key={r.id} value={r.id}>
                Ruta {r.code}
              </option>
            ))}
          </select>
        )}
      </div>
      <QueryState loading={detail.isPending} error={detail.error} inline>
        {d &&
          (d.rows.length === 0 ? (
            <div className="empty">{card.source ?? 'No hay nada que contar en este periodo.'}</div>
          ) : (
            <div className="table-wrap kpi-detail__table">
              <table>
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Ruta</th>
                    <th>Invitado o lugar</th>
                    <th>Detalle</th>
                    <th className="end">Suma</th>
                    {showDenominator && <th className="end">Cuenta en total</th>}
                  </tr>
                </thead>
                <tbody>
                  {d.rows.map((r, i) => (
                    <tr key={i}>
                      <td>{shortDay(r.date)}</td>
                      <td>{r.rutaCode ?? '—'}</td>
                      <td>
                        {r.pedidoId && canSeePedidos ? (
                          <Link to={`/pedidos?pedido=${r.pedidoId}`}>{r.name ?? `Pedido ${r.pedidoId}`}</Link>
                        ) : (
                          (r.name ?? '—')
                        )}
                      </td>
                      <td>{r.text}</td>
                      <td className="end">{card.unit === 'Pesos' ? formatValue('Pesos', r.numerator) : r.numerator}</td>
                      {showDenominator && <td className="end">{r.denominator}</td>}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
      </QueryState>
    </Dialog>
  );
}

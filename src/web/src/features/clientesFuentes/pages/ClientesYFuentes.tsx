import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { useSession } from '../../../shared/session/Session';
import { addDays, todayInMexicoCity } from '../../../shared/utils/dates';
import { Indicators } from '../components/Indicators';
import { PerdidosDialog } from '../components/PerdidosDialog';
import { Retencion } from '../components/Retencion';
import { clientesFuentesApi, clientesFuentesKey } from '../services/clientesFuentesApi';
import { money, percent, type Period, PERIODS, type Tipo, TIPOS } from '../types/clientesFuentes';
import '../clientesFuentes.css';

/**
 * Clientes y fuentes (docs/pantallas/oficina/clientes-y-fuentes.md): nuevos, recurrentes, recuperados, perdidos and
 * venta of a period, by source (cómo nos conoció), the trend of the last 6 periods and the retention of each month's
 * nuevos. Everything is calculated in the API with the same 120 days as the CRM.
 */
export function ClientesYFuentes() {
  const canEdit = useSession().can('clientes-y-fuentes', 'edit');
  const [periodo, setPeriodo] = useState<Period>('Mes');
  const [tipo, setTipo] = useState<Tipo>('Todos');
  const [fecha, setFecha] = useState(todayInMexicoCity);
  const [perdidos, setPerdidos] = useState<{ fuente?: string } | null>(null);
  const query = useQuery({
    queryKey: [...clientesFuentesKey, periodo, fecha, tipo],
    queryFn: () => clientesFuentesApi.period({ periodo, fecha, tipo }),
    placeholderData: keepPreviousData,
  });
  const d = query.data;
  const today = todayInMexicoCity();

  return (
    <>
      <PageHeader
        title="Clientes y fuentes"
        description={
          d ? (
            <>
              <strong>{d.label}</strong>
              {d.isPartial ? ` · en curso, hasta hoy; comparado con ${d.comparedLabel}` : ` · comparado con ${d.comparedLabel}`}
            </>
          ) : (
            'De dónde vienen los invitados y cuánto venden.'
          )
        }
      >
        <div className="segmented" role="group" aria-label="Tipo de invitado">
          {TIPOS.map((t) => (
            <button key={t.key} type="button" className={`segmented__option${tipo === t.key ? ' selected' : ''}`} onClick={() => setTipo(t.key)}>
              {t.name}
            </button>
          ))}
        </div>
        <div className="segmented" role="group" aria-label="Periodo">
          {PERIODS.map((p) => (
            <button key={p.key} type="button" className={`segmented__option${periodo === p.key ? ' selected' : ''}`} onClick={() => setPeriodo(p.key)}>
              {p.name}
            </button>
          ))}
        </div>
        <div className="actions tight">
          <button type="button" className="small" aria-label="Periodo anterior" disabled={!d} onClick={() => d && setFecha(addDays(d.start, -1))}>
            ‹
          </button>
          <button type="button" className="small" aria-label="Periodo siguiente" disabled={!d || d.end >= today} onClick={() => d && setFecha(addDays(d.end, 1))}>
            ›
          </button>
          {d && d.end < today && (
            <button type="button" className="small" onClick={() => setFecha(today)}>
              Actual
            </button>
          )}
        </div>
      </PageHeader>

      <QueryState loading={query.isPending} error={query.error}>
        {d && (
          <>
            {d.isPartial && <div className="alert info">Periodo en curso: cuenta hasta hoy y se compara contra los mismos días del periodo anterior.</div>}
            <Indicators totals={d.totals} previous={d.previous} comparedLabel={d.comparedLabel} onPerdidos={() => setPerdidos({})} />

            <section className="card">
              <div className="card__header">
                <div>
                  <h2>Por fuente</h2>
                  <p className="muted">Cómo nos conoció cada invitado. Clic en los perdidos de una fuente para ver quiénes son.</p>
                </div>
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Fuente</th>
                      <th className="end">Nuevos</th>
                      <th className="end">Venta de nuevos</th>
                      <th className="end">Atendidos</th>
                      <th className="end">Pedidos</th>
                      <th className="end">Venta</th>
                      <th className="end">Ticket promedio</th>
                      <th className="end">% de la venta</th>
                      <th className="end">Perdidos</th>
                    </tr>
                  </thead>
                  <tbody>
                    {d.fuentes.map((f) => (
                      <tr key={f.fuente}>
                        <td>
                          {f.fuente}
                          {f.isDigital && <span className="badge brand"> Digital</span>}
                        </td>
                        <td className="end">{f.nuevos}</td>
                        <td className="end">{money(f.ventaNuevos)}</td>
                        <td className="end">{f.atendidos}</td>
                        <td className="end">{f.pedidos}</td>
                        <td className="end">{money(f.venta)}</td>
                        <td className="end">{money(f.ticketPromedio)}</td>
                        <td className="end">{percent(f.porcentajeVenta)}</td>
                        <td className="end">
                          {f.perdidos > 0 ? (
                            <button type="button" className="link" onClick={() => setPerdidos({ fuente: f.fuente })}>
                              {f.perdidos}
                            </button>
                          ) : (
                            0
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr>
                      <th>Total</th>
                      <th className="end">{d.totals.nuevos}</th>
                      <th className="end">{money(d.totals.ventaNuevos)}</th>
                      <th className="end">{d.totals.atendidos}</th>
                      <th className="end">{d.totals.pedidos}</th>
                      <th className="end">{money(d.totals.venta)}</th>
                      <th className="end">{money(d.totals.ticketPromedio)}</th>
                      <th className="end">{d.totals.venta > 0 ? '100 %' : '—'}</th>
                      <th className="end">{d.totals.perdidos}</th>
                    </tr>
                  </tfoot>
                </table>
                {d.fuentes.length === 0 && <div className="empty">Sin invitados atendidos ni perdidos en este periodo.</div>}
              </div>
            </section>

            <section className="card">
              <div className="card__header">
                <h2>Tendencia</h2>
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Periodo</th>
                      <th className="end">Nuevos</th>
                      <th className="end">Recurrentes</th>
                      <th className="end">Recuperados</th>
                      <th className="end">Perdidos</th>
                      <th className="end">Atendidos</th>
                      <th className="end">Activos al cierre</th>
                      <th className="end">Venta</th>
                    </tr>
                  </thead>
                  <tbody>
                    {d.trend.map((t) => (
                      <tr key={t.start}>
                        <td>
                          {t.label}
                          {t.isPartial && <span className="muted"> (en curso)</span>}
                        </td>
                        <td className="end">{t.totals.nuevos}</td>
                        <td className="end">{t.totals.recurrentes}</td>
                        <td className="end">{t.totals.recuperados}</td>
                        <td className="end">{t.totals.perdidos}</td>
                        <td className="end">{t.totals.atendidos}</td>
                        <td className="end">{t.totals.activosAlCierre}</td>
                        <td className="end">{money(t.totals.venta)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <Retencion fecha={fecha} tipo={tipo} />

            <section className="card cyf-definitions">
              <h2>Definiciones</h2>
              <ul>
                <li>
                  <strong>Pedido:</strong> un pedido no cancelado, en su fecha de recolección (la misma que usa el CRM).
                </li>
                <li>
                  <strong>Nuevo:</strong> su primer pedido cae en el periodo.
                </li>
                <li>
                  <strong>Recurrente:</strong> su pedido anterior fue hace {d.inactiveDays} días o menos.
                </li>
                <li>
                  <strong>Recuperado:</strong> vuelve tras más de {d.inactiveDays} días sin pedir.
                </li>
                <li>
                  <strong>Perdido:</strong> cumple más de {d.inactiveDays} días sin pedir dentro del periodo (el mismo "Inactivo" del CRM).
                </li>
                <li>
                  <strong>Activo al cierre:</strong> pidió en los {d.inactiveDays} días previos al cierre del periodo.
                </li>
                <li>
                  <strong>Nuevos de marketing digital:</strong> nuevos cuya fuente está marcada como marketing digital en Operación (catálogo "Cómo nos conoció"). La campaña escrita después de « · » no cuenta para la fuente.
                </li>
                <li>
                  <strong>Venta:</strong> importe de los tickets del punto de venta (mientras no se conecta, los capturados en Tickets en planta). Sin tiendas internas.
                </li>
              </ul>
            </section>
          </>
        )}
      </QueryState>

      {perdidos && (
        <PerdidosDialog periodo={periodo} fecha={fecha} tipo={tipo} fuente={perdidos.fuente} canEdit={canEdit} onClose={() => setPerdidos(null)} />
      )}
    </>
  );
}

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { useSession } from '../../../shared/session/Session';
import { addDays, formatDateTime, todayInMexicoCity } from '../../../shared/utils/dates';
import { KpiCardView } from '../components/KpiCard';
import { KpiDetailDialog } from '../components/KpiDetail';
import { KpiHistoryTable } from '../components/KpiHistory';
import { kpisApi } from '../services/kpisApi';
import { kpisKey } from '../services/queryKeys';
import { AREAS, type KpiCard, type KpiPeriod, LIGHTS, PERIODS } from '../types/kpis';
import '../kpis.css';

/**
 * KPIs (docs/pantallas/oficina/kpis.md): every indicator of the week, month or quarter with its goal and light, the
 * desglose by ruta and the last 8 weeks. Values, periods and lights come calculated from the API.
 */
export function Kpis() {
  const canEdit = useSession().can('kpis', 'edit');
  const [periodo, setPeriodo] = useState<KpiPeriod>('Semana');
  const [fecha, setFecha] = useState(todayInMexicoCity);
  const [open, setOpen] = useState<KpiCard | null>(null);
  const board = useQuery({ queryKey: [...kpisKey, periodo, fecha], queryFn: () => kpisApi.board({ periodo, fecha }) });
  const history = useQuery({ queryKey: [...kpisKey, 'historico'], queryFn: () => kpisApi.history({ semanas: 8 }) });
  const recalculate = useSave(() => kpisApi.recalculate({ periodo, fecha }), { refetch: [kpisKey], message: 'KPIs recalculados.' });
  const b = board.data;

  return (
    <>
      <PageHeader
        title="KPIs"
        description={
          b ? (
            <>
              <strong>{b.label}</strong> · comparado con {b.previousLabel.charAt(0).toLowerCase() + b.previousLabel.slice(1)}
              {b.calculatedAt && <> · calculado {formatDateTime(b.calculatedAt)}</>}
            </>
          ) : (
            'Indicadores con sus metas por semana, mes o trimestre.'
          )
        }
      >
        <div className="segmented" role="group" aria-label="Periodo">
          {PERIODS.map((p) => (
            <button
              key={p.key}
              type="button"
              className={`segmented__option ${periodo === p.key ? 'selected' : ''}`}
              onClick={() => setPeriodo(p.key)}
            >
              {p.name}
            </button>
          ))}
        </div>
        <div className="actions">
          <button type="button" className="small" disabled={!b} onClick={() => b && setFecha(addDays(b.start, -1))} aria-label="Periodo anterior">
            ‹
          </button>
          <button type="button" className="small" disabled={!b || b.isCurrent} onClick={() => setFecha(todayInMexicoCity())}>
            Hoy
          </button>
          <button type="button" className="small" disabled={!b} onClick={() => b && setFecha(addDays(b.end, 1))} aria-label="Periodo siguiente">
            ›
          </button>
        </div>
        {canEdit && (
          <button type="button" className="small" disabled={recalculate.isPending} onClick={() => recalculate.mutate(undefined)}>
            {recalculate.isPending ? 'Recalculando…' : 'Recalcular'}
          </button>
        )}
      </PageHeader>

      <QueryState loading={board.isPending} error={board.error ?? recalculate.error}>
        {b && (
          <div className="stack kpi-page">
            <div className="kpi-counts">
              {LIGHTS.map((l) => (
                <div key={l.key} className={`kpi-count light-${l.key}`}>
                  <span>{l.name}</span>
                  <strong>{b.counts[countKey(l.key)]}</strong>
                </div>
              ))}
            </div>
            {AREAS.map((area) => (
              <section key={area.key} className="kpi-area">
                <h2>{area.name}</h2>
                <div className="kpi-grid">
                  {b.indicadores
                    .filter((c) => c.area === area.key)
                    .map((c) => (
                      <KpiCardView key={c.key} card={c} previousLabel={b.previousLabel} onOpen={() => setOpen(c)} />
                    ))}
                </div>
              </section>
            ))}
            <section className="card">
              <div className="card__header">
                <h2>Últimas 8 semanas</h2>
                <span className="muted">Cada semana con la meta que tenía.</span>
              </div>
              <QueryState loading={history.isPending} error={history.error} inline>
                {history.data && <KpiHistoryTable history={history.data} />}
              </QueryState>
            </section>
            <div className="alert">{b.externalSources}</div>
          </div>
        )}
      </QueryState>
      {open && b && <KpiDetailDialog card={open} periodo={periodo} fecha={fecha} label={b.label} rutas={b.rutas} onClose={() => setOpen(null)} />}
    </>
  );
}

const countKey = (light: KpiCard['light']) =>
  ({ SuperVerde: 'superVerde', Verde: 'verde', Rojo: 'rojo', SinMeta: 'sinMeta' })[light] as 'superVerde' | 'verde' | 'rojo' | 'sinMeta';

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
import { AREAS, type KpiCard, type KpiLight, type KpiPeriod, LIGHTS, lightName, PERIODS } from '../types/kpis';
import '../kpis.css';

/**
 * KPIs (docs/pantallas/oficina/kpis.md): every indicator of the week, month or quarter, of the whole company or of one
 * ruta, with its goal, light, progress and last 8 weeks. Clicking a counter leaves only the indicators of that color;
 * within each area the red ones go first, and those that never had data go in one line. "Imprimir" gives a version for
 * the meeting (paper or PDF).
 * Values, goals, lights and progress come calculated from the API.
 */
export function Kpis() {
  const canEdit = useSession().can('kpis', 'edit');
  const [periodo, setPeriodo] = useState<KpiPeriod>('Semana');
  const [fecha, setFecha] = useState(todayInMexicoCity);
  const [rutaId, setRutaId] = useState<number | undefined>();
  const [onlyLight, setOnlyLight] = useState<KpiLight | null>(null);
  const [showTable, setShowTable] = useState(false);
  const [open, setOpen] = useState<KpiCard | null>(null);
  const board = useQuery({ queryKey: [...kpisKey, periodo, fecha, rutaId], queryFn: () => kpisApi.board({ periodo, fecha, rutaId }) });
  const history = useQuery({
    queryKey: [...kpisKey, 'historico', rutaId],
    queryFn: () => kpisApi.history({ semanas: 8, rutaId }),
    enabled: showTable,
  });
  const recalculate = useSave(() => kpisApi.recalculate({ periodo, fecha, rutaId }), { refetch: [kpisKey], message: 'KPIs recalculados.' });
  const b = board.data;
  const ruta = b?.rutas.find((r) => r.id === rutaId);
  const cards = b?.indicadores.filter((c) => !onlyLight || c.light === onlyLight) ?? [];
  // Indicators that never had a value (they wait for the survey or the POS) go in one line, not in empty cards.
  const withoutData = cards.filter(hasNoData);
  const withData = cards.filter((c) => !hasNoData(c));
  const rutaOptions = b
    ? [{ id: undefined as number | undefined, name: 'Todas' }, ...b.rutas.map((r) => ({ id: r.id as number | undefined, name: r.code }))]
    : [];

  return (
    <>
      <PageHeader
        title={ruta ? `KPIs · Ruta ${ruta.code}` : 'KPIs'}
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
        {rutaOptions.length > 2 && (
          <div className="segmented" role="group" aria-label="Ruta">
            {rutaOptions.map((r) => (
              <button key={r.name} type="button" className={`segmented__option ${rutaId === r.id ? 'selected' : ''}`} onClick={() => setRutaId(r.id)}>
                {r.name}
              </button>
            ))}
          </div>
        )}
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
        <button type="button" className="small" disabled={!b} onClick={() => window.print()} title="Imprimir o guardar en PDF para la junta">
          Imprimir
        </button>
        {canEdit && (
          <button type="button" className="small" disabled={recalculate.isPending} onClick={() => recalculate.mutate(undefined)}>
            {recalculate.isPending ? 'Recalculando…' : 'Recalcular'}
          </button>
        )}
      </PageHeader>

      <QueryState loading={board.isPending} error={board.error ?? recalculate.error}>
        {b && (
          <div className="stack kpi-page">
            <div className="kpi-counts" role="group" aria-label="Filtrar por color">
              {LIGHTS.map((l) => (
                <button
                  key={l.key}
                  type="button"
                  className={`kpi-count light-${l.key} ${onlyLight === l.key ? 'selected' : ''}`}
                  aria-pressed={onlyLight === l.key}
                  onClick={() => setOnlyLight((x) => (x === l.key ? null : l.key))}
                  title={onlyLight === l.key ? 'Ver todos' : `Ver solo ${l.name.toLowerCase()}`}
                >
                  <span>{l.name}</span>
                  <strong>{b.counts[countKey(l.key)]}</strong>
                </button>
              ))}
            </div>
            {onlyLight && (
              <div className="kpi-filter">
                Mostrando solo <strong>{lightName(onlyLight).toLowerCase()}</strong> ({cards.length}).{' '}
                <button type="button" className="link" onClick={() => setOnlyLight(null)}>
                  Ver todos
                </button>
              </div>
            )}
            {AREAS.map((area) => {
              const ofArea = withData.filter((c) => c.area === area.key).sort((x, y) => ATTENTION[x.light] - ATTENTION[y.light]);
              if (ofArea.length === 0) return null;
              return (
                <section key={area.key} className="kpi-area">
                  <h2>{area.name}</h2>
                  <div className="kpi-grid">
                    {ofArea.map((c) => (
                      <KpiCardView key={c.key} card={c} weeks={b.weeks} previousLabel={b.previousLabel} onOpen={() => setOpen(c)} />
                    ))}
                  </div>
                </section>
              );
            })}
            {withoutData.length > 0 && (
              <div className="kpi-no-data">
                <strong>Sin datos todavía:</strong>{' '}
                {withoutData.map((c, i) => (
                  <span key={c.key}>
                    {i > 0 && ' · '}
                    <button type="button" className="link" onClick={() => setOpen(c)} title={c.source ?? c.calculation}>
                      {c.name}
                    </button>
                    {c.source && <span className="muted"> ({c.source.replace(/.$/, '')})</span>}
                  </span>
                ))}
              </div>
            )}
            {cards.length === 0 && onlyLight && <div className="empty">Ningún indicador en {lightName(onlyLight).toLowerCase()}.</div>}
            <section className="card">
              <button type="button" className="card__header kpi-table-toggle" aria-expanded={showTable} onClick={() => setShowTable((x) => !x)}>
                <h2>
                  {showTable ? '▾' : '▸'} Tabla de las últimas 8 semanas{ruta && ` · Ruta ${ruta.code}`}
                </h2>
                <span className="muted">Cada semana con la meta que tenía.</span>
              </button>
              {showTable && (
                <QueryState loading={history.isPending} error={history.error} inline>
                  {history.data && <KpiHistoryTable history={history.data} />}
                </QueryState>
              )}
            </section>
            <div className="alert">{b.externalSources}</div>
          </div>
        )}
      </QueryState>
      {open && b && (
        <KpiDetailDialog card={open} periodo={periodo} fecha={fecha} label={b.label} rutas={b.rutas} rutaId={rutaId} onClose={() => setOpen(null)} />
      )}
    </>
  );
}

/** Within each area: first what needs attention. */
const ATTENTION: Record<KpiCard['light'], number> = { Rojo: 0, Verde: 1, SuperVerde: 2, SinMeta: 3 };

const hasNoData = (c: KpiCard) => c.value == null && c.previous == null && c.weeks.every((w) => w.value == null);

const countKey = (light: KpiCard['light']) =>
  ({ SuperVerde: 'superVerde', Verde: 'verde', Rojo: 'rojo', SinMeta: 'sinMeta' })[light] as 'superVerde' | 'verde' | 'rojo' | 'sinMeta';

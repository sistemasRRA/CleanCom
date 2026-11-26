import { KpiProgressBar } from './KpiProgressBar';
import { formatValue, goalPeriodName, type KpiBoard, type KpiCard, lightName } from '../types/kpis';

/**
 * One indicator: value with its light, the bar against its goal and how much is missing, the period before, the last 8
 * weeks, the desglose by ruta, the goal and who answers for it.
 */
export function KpiCardView({
  card: c,
  weeks,
  previousLabel,
  onOpen,
}: {
  card: KpiCard;
  weeks: KpiBoard['weeks'];
  previousLabel: string;
  onOpen: () => void;
}) {
  const better = c.trend === 'Sube' ? c.higherIsBetter : c.trend === 'Baja' ? !c.higherIsBetter : null;
  const g = c.goal;
  const thresholds = g
    ? [
        g.red != null && `Rojo ${formatValue(c.unit, g.red)}`,
        `Verde ${formatValue(c.unit, g.green)}`,
        g.superGreen != null && `Súper verde ${formatValue(c.unit, g.superGreen)}`,
      ]
        .filter(Boolean)
        .join(' · ')
    : '';
  const origin = !g
    ? ''
    : g.fromRutas
      ? 'Suma de las metas por ruta'
      : g.scaled && g.period
        ? `Meta ${goalPeriodName(g.period).toLowerCase()}, proporcional al periodo`
        : 'Meta';
  return (
    <button type="button" className={`kpi-card light-${c.light}`} onClick={onOpen} title="Ver lo que forma este indicador">
      <div className="kpi-card__top">
        <span className="kpi-card__name">{c.name}</span>
        <span className={`kpi-light light-${c.light}`}>{lightName(c.light)}</span>
      </div>
      <div className="kpi-card__main">
        <strong className="kpi-card__value">{formatValue(c.unit, c.value)}</strong>
        <Sparkline card={c} weeks={weeks} />
      </div>
      <div className="kpi-card__previous">
        {c.trend !== 'SinDatos' && c.trend !== 'Igual' && (
          <span className={better ? 'kpi-up' : 'kpi-down'} aria-hidden>
            {c.trend === 'Sube' ? '▲' : '▼'}
          </span>
        )}{' '}
        <span title={previousLabel}>Anterior: {formatValue(c.unit, c.previous)}</span>
      </div>
      {c.progress && <KpiProgressBar progress={c.progress} unit={c.unit} higherIsBetter={c.higherIsBetter} light={c.light} />}
      {c.byRuta.length > 1 && (
        <div className="kpi-card__rutas">
          {c.byRuta.map((r) => (
            <span
              key={r.rutaId}
              className={`kpi-ruta light-${r.light}`}
              title={r.green != null ? `Meta verde de la ruta ${r.code}: ${formatValue(c.unit, r.green)}` : `La ruta ${r.code} no tiene meta`}
            >
              {r.code} {formatValue(c.unit, r.value)}
            </span>
          ))}
        </div>
      )}
      <div className="kpi-card__goal">
        {g ? (
          <>
            <span>{thresholds}</span>
            <span className="muted">
              {origin}
              {g.responsable && ` · ${g.responsable}`}
            </span>
          </>
        ) : (
          <span className="muted">{c.goalNote ?? 'Sin meta: se captura en WhatsApp y metas.'}</span>
        )}
      </div>
      {c.source && <div className="kpi-card__source">{c.source}</div>}
    </button>
  );
}

/** The last 8 weeks as bars, each with the color of the goal it had; hovering shows the week and the value. */
function Sparkline({ card: c, weeks }: { card: KpiCard; weeks: KpiBoard['weeks'] }) {
  const values = c.weeks.map((w) => w.value);
  const known = values.filter((v): v is number => v != null);
  if (known.length === 0) return null;
  const min = Math.min(0, ...known);
  const max = Math.max(...known);
  const height = (v: number) => (max === min ? 12 : 3 + ((v - min) / (max - min)) * 21);
  return (
    <svg className="kpi-spark" viewBox="0 0 80 24" width="80" height="24" role="img" aria-label="Últimas 8 semanas">
      {c.weeks.map((w, i) => (
        <rect
          key={i}
          className={`kpi-spark__bar light-${w.light}`}
          x={i * 10 + 1}
          width="8"
          y={w.value == null ? 22 : 24 - height(w.value)}
          height={w.value == null ? 2 : height(w.value)}
          rx="1.5"
        >
          <title>{`Semana del ${weeks[i]?.label ?? ''}: ${formatValue(c.unit, w.value)} · ${lightName(w.light)}`}</title>
        </rect>
      ))}
    </svg>
  );
}

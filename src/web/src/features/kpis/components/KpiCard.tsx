import { formatValue, goalPeriodName, type KpiCard, lightName } from '../types/kpis';

/** One indicator: value with its light, the period before, the desglose by ruta, the goal and who answers for it. */
export function KpiCardView({ card: c, previousLabel, onOpen }: { card: KpiCard; previousLabel: string; onOpen: () => void }) {
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
      <strong className="kpi-card__value">{formatValue(c.unit, c.value)}</strong>
      <div className="kpi-card__previous">
        {c.trend !== 'SinDatos' && c.trend !== 'Igual' && (
          <span className={better ? 'kpi-up' : 'kpi-down'} aria-hidden>
            {c.trend === 'Sube' ? '▲' : '▼'}
          </span>
        )}{' '}
        <span title={previousLabel}>Anterior: {formatValue(c.unit, c.previous)}</span>
      </div>
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

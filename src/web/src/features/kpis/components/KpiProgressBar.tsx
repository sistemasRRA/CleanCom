import { formatValue, type KpiLight, type KpiProgress, type KpiUnit } from '../types/kpis';

/**
 * The value against rojo, verde and súper verde, as the API placed it (the good side is always on the right), and how
 * much is missing for the next color. <paramref name="compact"/> leaves only the bar, for tables.
 */
export function KpiProgressBar({
  progress: p,
  unit,
  higherIsBetter,
  light,
  compact,
}: {
  progress: KpiProgress;
  unit: KpiUnit;
  higherIsBetter: boolean;
  light: KpiLight;
  compact?: boolean;
}) {
  const green = p.greenPosition;
  const superGreen = p.superGreenPosition ?? 100;
  const color = p.next === 'SuperVerde' ? 'súper verde' : 'verde';
  const gap = formatValue(unit, p.gap);
  const text = !p.next
    ? light === 'SuperVerde'
      ? 'Súper verde alcanzado'
      : 'Meta alcanzada'
    : higherIsBetter
      ? `Faltan ${gap} para ${color}`
      : `Hay que bajar ${gap} para ${color}`;
  return (
    <div className={`kpi-progress ${compact ? 'compact' : ''}`} title={compact ? text : undefined}>
      <div
        className="kpi-progress__bar"
        style={{
          background: `linear-gradient(to right, var(--kpi-red) 0 ${green}%, var(--kpi-green) ${green}% ${superGreen}%, var(--kpi-super) ${superGreen}% 100%)`,
        }}
        role="img"
        aria-label={text}
      >
        <span className="kpi-progress__marker" style={{ left: `${Math.min(100, Math.max(0, p.valuePosition))}%` }} />
      </div>
      {!compact && <span className={`kpi-progress__text ${p.next ? '' : 'done'}`}>{text}</span>}
    </div>
  );
}

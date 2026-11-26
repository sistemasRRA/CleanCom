import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { kpisApi } from '../services/kpisApi';
import { metasKey } from '../services/queryKeys';
import { formatValue, type KpiGoalPeriod, type KpiGoalRow, lightName } from '../types/kpis';
import { KpiProgressBar } from './KpiProgressBar';

/** The values of a goal being typed (as text, empty when not given). */
export type GoalDraft = { period: KpiGoalPeriod; red: string; green: string; superGreen: string };

const optional = (s: string) => (s.trim() === '' ? null : Number(s));

/** Waits until typing stops, so the preview is not asked on every key. */
function useSettled<T>(value: T, ms = 400) {
  const [settled, setSettled] = useState(value);
  const key = JSON.stringify(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setSettled(JSON.parse(key) as T), ms);
    return () => window.clearTimeout(timer);
  }, [key, ms]);
  return settled;
}

/**
 * A goal being edited tried on last week, as the API calculates it: the color it would have given, the bar, what it
 * equals per day, week, month and quarter, or why it cannot be saved.
 */
export function GoalPreview({ row: r, rutaId, draft }: { row: KpiGoalRow; rutaId: number | null; draft: GoalDraft }) {
  const settled = useSettled(draft);
  const preview = useQuery({
    queryKey: [...metasKey, 'previsualizar', r.key, rutaId, settled],
    queryFn: () =>
      kpisApi.previewGoal(r.key, {
        period: settled.period,
        red: optional(settled.red),
        green: Number(settled.green),
        superGreen: optional(settled.superGreen),
        responsableUserId: null,
        validFrom: null,
        rutaId,
      }),
    enabled: settled.green.trim() !== '',
    placeholderData: (previous) => previous,
  });
  const p = preview.data;
  if (draft.green.trim() === '') return <div className="kpi-preview muted">Escribe al menos el verde para ver cómo habría quedado.</div>;
  if (preview.error) return <div className="kpi-preview error">{preview.error.message}</div>;
  if (!p) return <div className="kpi-preview muted">Calculando…</div>;
  if (p.error) return <div className="kpi-preview error">{p.error}</div>;
  const week = p.lastWeekLabel.charAt(0).toLowerCase() + p.lastWeekLabel.slice(1);

  return (
    <div className="kpi-preview">
      <div>
        {p.value == null ? (
          <>La {week} no tuvo datos para comparar.</>
        ) : p.note ? (
          <>{p.note}</>
        ) : (
          <>
            Con estos valores, la {week} habría sido <span className={`kpi-light light-${p.light}`}>{lightName(p.light)}</span>:{' '}
            {formatValue(r.unit, p.value)} contra verde {formatValue(r.unit, p.thresholds?.green)}.
          </>
        )}
      </div>
      {p.progress && <KpiProgressBar progress={p.progress} unit={r.unit} higherIsBetter={r.higherIsBetter} light={p.light} />}
      {p.equivalences.length > 0 && (
        <div className="muted">Verde equivale a {p.equivalences.map((e) => `${formatValue(r.unit, e.green)} ${e.label}`).join(' · ')}.</div>
      )}
    </div>
  );
}

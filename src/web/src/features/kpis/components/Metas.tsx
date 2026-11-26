import { Fragment, type ReactNode, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Checkbox } from '../../../shared/components/Form';
import { QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { useSession } from '../../../shared/session/Session';
import { shortDay } from '../../../shared/utils/dates';
import { kpisApi } from '../services/kpisApi';
import { kpisKey, metasKey } from '../services/queryKeys';
import {
  AREAS,
  formatValue,
  GOAL_PERIODS,
  goalPeriodName,
  type KpiArea,
  type KpiGoal,
  type KpiGoalCheck,
  type KpiGoalPeriod,
  type KpiGoalRow,
  type KpiGoals,
  lightName,
} from '../types/kpis';
import { MetaForm } from './MetaForm';

/** What is open in the full form: an indicator, for the whole company (rutaId null) or for one ruta. */
type Editing = { row: KpiGoalRow; rutaId: number | null };

const hasGoal = (r: KpiGoalRow) => !!r.current || r.rutas.some((x) => x.current);
const lacksResponsable = (r: KpiGoalRow) => [r.current, ...r.rutas.map((x) => x.current)].some((g) => g && !g.responsable);

/**
 * Metas de los KPIs (WhatsApp y metas › Metas): rojo, verde, súper verde and responsable captured in the row and saved
 * from today, for the whole company and, opening "Por ruta", for each ruta; next to it, how the indicator did last week.
 * A start date later than today and the history are in the full form ("Más").
 */
export function Metas() {
  const canEdit = useSession().can('whatsapp-y-metas', 'edit');
  const [editing, setEditing] = useState<Editing | null>(null);
  const [area, setArea] = useState<KpiArea | null>(null);
  const [withoutGoal, setWithoutGoal] = useState(false);
  const [withoutResponsable, setWithoutResponsable] = useState(false);
  const [open, setOpen] = useState<Record<string, boolean>>({});
  const list = useQuery({ queryKey: metasKey, queryFn: () => kpisApi.goals() });
  const all = list.data?.indicadores ?? [];
  const rows = all.filter((r) => (!area || r.area === area) && (!withoutGoal || !hasGoal(r)) && (!withoutResponsable || lacksResponsable(r)));
  const isOpen = (r: KpiGoalRow) => open[r.key] ?? r.rutas.some((x) => x.current || x.next);

  return (
    <QueryState loading={list.isPending} error={list.error}>
      <div className="kpi-goals__bar">
        <div className="segmented" role="group" aria-label="Área">
          {[{ key: null, name: 'Todas' }, ...AREAS].map((a) => (
            <button
              key={a.key ?? 'todas'}
              type="button"
              className={`segmented__option ${area === a.key ? 'selected' : ''}`}
              onClick={() => setArea(a.key)}
            >
              {a.name}
            </button>
          ))}
        </div>
        <Checkbox checked={withoutGoal} onChange={setWithoutGoal}>
          Sin meta ({all.filter((r) => !hasGoal(r)).length})
        </Checkbox>
        <Checkbox checked={withoutResponsable} onChange={setWithoutResponsable}>
          Sin responsable ({all.filter(lacksResponsable).length})
        </Checkbox>
        <span className="muted kpi-goals__help">
          Verde es obligatorio; rojo y súper verde, opcionales. Guardar aplica desde hoy: las semanas pasadas conservan su meta.
        </span>
      </div>
      <section className="card">
        <div className="table-wrap">
          <table className="kpi-goals">
            <thead>
              <tr>
                <th>Indicador</th>
                <th>La meta es</th>
                <th className="end">Rojo</th>
                <th className="end">Verde</th>
                <th className="end">Súper verde</th>
                <th>Responsable</th>
                <th className="end" title={list.data?.lastWeekLabel}>
                  Semana pasada
                </th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 && (
                <tr>
                  <td colSpan={8} className="empty">
                    Ningún indicador con esos filtros.
                  </td>
                </tr>
              )}
              {rows.map((r) => (
                <Fragment key={r.key}>
                  <GoalLine
                    key={r.current?.id ?? 'nueva'}
                    row={r}
                    rutaId={null}
                    goal={r.current}
                    next={r.next}
                    lastWeek={r.lastWeek}
                    responsables={list.data!.responsables}
                    canEdit={canEdit}
                    onMore={() => setEditing({ row: r, rutaId: null })}
                    toggle={
                      r.rutas.length > 0 && (
                        <button
                          type="button"
                          className="link small kpi-goals__toggle"
                          onClick={() => setOpen((o) => ({ ...o, [r.key]: !isOpen(r) }))}
                        >
                          {isOpen(r) ? '▾' : '▸'} Por ruta
                          {r.rutas.some((x) => x.current) &&
                            ` (${r.rutas
                              .filter((x) => x.current)
                              .map((x) => x.code)
                              .join(', ')})`}
                        </button>
                      )
                    }
                  />
                  {isOpen(r) &&
                    r.rutas.map((x) => (
                      <GoalLine
                        key={`${x.rutaId}-${x.current?.id ?? 'nueva'}`}
                        row={r}
                        rutaId={x.rutaId}
                        label={`Ruta ${x.code}`}
                        goal={x.current}
                        next={x.next}
                        lastWeek={x.lastWeek}
                        responsables={list.data!.responsables}
                        canEdit={canEdit}
                        onMore={() => setEditing({ row: r, rutaId: x.rutaId })}
                      />
                    ))}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      {editing && list.data && (
        <MetaForm row={editing.row} rutaId={editing.rutaId} responsables={list.data.responsables} onClose={() => setEditing(null)} />
      )}
    </QueryState>
  );
}

const text = (n?: number | null) => (n == null ? '' : String(n));
const optional = (s: string) => (s.trim() === '' ? null : Number(s));

/** One goal edited in place: the company's (with the indicator's name) or a ruta's. Saving applies from today. */
function GoalLine({
  row: r,
  rutaId,
  label,
  goal: g,
  next,
  lastWeek,
  responsables,
  canEdit,
  onMore,
  toggle,
}: {
  row: KpiGoalRow;
  rutaId: number | null;
  label?: string;
  goal: KpiGoal | null;
  next: KpiGoal | null;
  lastWeek: KpiGoalCheck;
  responsables: KpiGoals['responsables'];
  canEdit: boolean;
  onMore: () => void;
  toggle?: ReactNode;
}) {
  const periods = r.scaling === 'SamePeriod' ? GOAL_PERIODS.filter((p) => p.key !== 'Dia') : GOAL_PERIODS;
  const initial = {
    period: periods.find((p) => p.key === g?.period)?.key ?? ('Semana' as KpiGoalPeriod),
    red: text(g?.red),
    green: text(g?.green),
    superGreen: text(g?.superGreen),
    responsable: g?.responsableUserId ? String(g.responsableUserId) : '',
  };
  const [v, setV] = useState(initial);
  const [savedFrom, setSavedFrom] = useState(initial);
  const dirty = JSON.stringify(v) !== JSON.stringify(savedFrom);
  const save = useSave(
    () =>
      kpisApi.saveGoal(r.key, {
        period: v.period,
        red: optional(v.red),
        green: Number(v.green),
        superGreen: optional(v.superGreen),
        responsableUserId: v.responsable ? Number(v.responsable) : null,
        validFrom: null,
        rutaId,
      }),
    { refetch: [metasKey, kpisKey], message: `Meta guardada: ${r.name}${label ? ` · ${label}` : ''}.`, onDone: () => setSavedFrom(v) },
  );
  const step = r.unit === 'Pesos' ? '1' : '0.01';
  const number = (key: 'red' | 'green' | 'superGreen', name: string) =>
    canEdit ? (
      <input
        type="number"
        step={step}
        className="kpi-goals__input"
        aria-label={`${name} · ${r.name}${label ? ` · ${label}` : ''}`}
        value={v[key]}
        onChange={(e) => setV({ ...v, [key]: e.target.value })}
      />
    ) : (
      formatValue(r.unit, optional(v[key]))
    );

  return (
    <tr className={rutaId == null ? 'kpi-goals__company' : 'kpi-goals__ruta'}>
      <td>
        {rutaId == null ? (
          <>
            <strong>{r.name}</strong>{' '}
            <span className="kpi-goals__info" title={r.calculation} aria-label={r.calculation}>
              ⓘ
            </span>
            <div className="muted">
              {AREAS.find((a) => a.key === r.area)?.name} · {r.higherIsBetter ? 'más es mejor' : 'menos es mejor'}
            </div>
          </>
        ) : (
          <span className="kpi-goals__ruta-name">{label}</span>
        )}
        {next && (
          <div className="muted">
            Cambia el {shortDay(next.validFrom)} (verde {formatValue(r.unit, next.green)}).
          </div>
        )}
        {toggle}
      </td>
      <td>
        {r.scaling === 'None' ? (
          <span className="muted" title="Es un promedio o un porcentaje: la meta vale igual por semana, mes o trimestre.">
            Cualquiera
          </span>
        ) : canEdit ? (
          <select
            className="kpi-goals__input"
            value={v.period}
            aria-label={`Periodo · ${r.name}`}
            onChange={(e) => setV({ ...v, period: e.target.value as KpiGoalPeriod })}
          >
            {periods.map((p) => (
              <option key={p.key} value={p.key}>
                {p.name}
              </option>
            ))}
          </select>
        ) : (
          goalPeriodName(v.period)
        )}
      </td>
      <td className="end">{number('red', 'Rojo')}</td>
      <td className="end">{number('green', 'Verde')}</td>
      <td className="end">{number('superGreen', 'Súper verde')}</td>
      <td>
        {canEdit ? (
          <select
            className="kpi-goals__input"
            value={v.responsable}
            aria-label={`Responsable · ${r.name}`}
            onChange={(e) => setV({ ...v, responsable: e.target.value })}
          >
            <option value="">Sin responsable</option>
            {responsables.map((u) => (
              <option key={u.id} value={u.id}>
                {u.name}
              </option>
            ))}
          </select>
        ) : (
          (g?.responsable ?? '—')
        )}
      </td>
      <td className="end">
        <span className="kpi-goals__last">{formatValue(r.unit, lastWeek.value)}</span>{' '}
        <span className={`kpi-light light-${lastWeek.light}`}>{lightName(lastWeek.light)}</span>
      </td>
      <td className="end kpi-goals__actions">
        {canEdit && (
          <>
            <button
              type="button"
              className="small primary"
              disabled={!dirty || v.green.trim() === '' || save.isPending}
              onClick={() => save.mutate(undefined)}
            >
              {save.isPending ? 'Guardando…' : 'Guardar'}
            </button>
            <button type="button" className="small" onClick={onMore} title="Fecha de inicio futura e historial">
              Más
            </button>
          </>
        )}
        {save.error && <div className="kpi-goals__error">{save.error.message}</div>}
      </td>
    </tr>
  );
}

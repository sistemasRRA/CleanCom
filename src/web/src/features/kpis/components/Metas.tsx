import { Fragment, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { QueryState } from '../../../shared/components/Page';
import { useAction, useSave } from '../../../shared/hooks/useSave';
import { useSession } from '../../../shared/session/Session';
import { shortDay, todayInMexicoCity } from '../../../shared/utils/dates';
import { kpisApi } from '../services/kpisApi';
import { kpisKey, metasKey } from '../services/queryKeys';
import { AREAS, formatValue, GOAL_PERIODS, goalPeriodName, type KpiGoal, type KpiGoalPeriod, type KpiGoalRow, type KpiGoals } from '../types/kpis';

/** What is being edited: an indicator, for the whole company (rutaId null) or for one ruta. */
type Editing = { row: KpiGoalRow; rutaId: number | null };

/**
 * Metas de los KPIs (WhatsApp y metas › Metas): rojo, verde y súper verde, period and responsable per indicator, for the
 * whole company or per ruta. A change applies from a date on (today or later); past periods keep the goal they had.
 */
export function Metas() {
  const canEdit = useSession().can('whatsapp-y-metas', 'edit');
  const [editing, setEditing] = useState<Editing | null>(null);
  const list = useQuery({ queryKey: metasKey, queryFn: () => kpisApi.goals() });

  return (
    <QueryState loading={list.isPending} error={list.error}>
      <div className="alert" style={{ marginBottom: 16 }}>
        Llega a verde quien alcanza la meta verde y a súper verde quien alcanza la súper verde; por debajo de verde es rojo. Rojo y súper verde son
        opcionales. Una meta puede ser de toda la empresa o de una ruta; sin meta de empresa, el total usa la suma de las metas por ruta. Una meta
        nueva aplica desde la fecha que se indique: los periodos pasados conservan la meta que tenían.
      </div>
      <section className="card">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Indicador</th>
                <th>Mejor</th>
                <th>Aplica a</th>
                <th>Periodo</th>
                <th className="end">Rojo</th>
                <th className="end">Verde</th>
                <th className="end">Súper verde</th>
                <th>Responsable</th>
                <th>Desde</th>
                <th></th>
              </tr>
            </thead>
            {AREAS.map((area) => (
              <tbody key={area.key}>
                <tr className="kpi-history__area">
                  <th colSpan={10}>{area.name}</th>
                </tr>
                {list.data?.indicadores
                  .filter((r) => r.area === area.key)
                  .map((r) => (
                    <Fragment key={r.key}>
                      <GoalRow
                        row={r}
                        goal={r.current}
                        next={r.next}
                        label="Empresa"
                        first
                        canEdit={canEdit}
                        onEdit={() => setEditing({ row: r, rutaId: null })}
                      />
                      {r.rutas
                        .filter((ruta) => ruta.current || ruta.next)
                        .map((ruta) => (
                          <GoalRow
                            key={ruta.rutaId}
                            row={r}
                            goal={ruta.current}
                            next={ruta.next}
                            label={`Ruta ${ruta.code}`}
                            canEdit={canEdit}
                            onEdit={() => setEditing({ row: r, rutaId: ruta.rutaId })}
                          />
                        ))}
                    </Fragment>
                  ))}
              </tbody>
            ))}
          </table>
        </div>
      </section>
      {editing && list.data && (
        <MetaForm row={editing.row} rutaId={editing.rutaId} responsables={list.data.responsables} onClose={() => setEditing(null)} />
      )}
    </QueryState>
  );
}

function GoalRow({
  row: r,
  goal: g,
  next,
  label,
  first,
  canEdit,
  onEdit,
}: {
  row: KpiGoalRow;
  goal: KpiGoal | null;
  next: KpiGoal | null;
  label: string;
  first?: boolean;
  canEdit: boolean;
  onEdit: () => void;
}) {
  return (
    <tr className={first ? '' : 'kpi-goal-ruta'}>
      <td>
        {first && (
          <>
            <strong>{r.name}</strong>
            <div className="muted">{r.calculation}</div>
          </>
        )}
        {next && (
          <div className="muted">
            Cambia el {shortDay(next.validFrom)}: verde {formatValue(r.unit, next.green)}.
          </div>
        )}
      </td>
      <td>{first ? (r.higherIsBetter ? 'Más' : 'Menos') : ''}</td>
      <td>{label}</td>
      <td>{g ? (r.scaling === 'None' ? 'Cualquiera' : goalPeriodName(g.period)) : '—'}</td>
      <td className="end">{g ? formatValue(r.unit, g.red) : '—'}</td>
      <td className="end">{g ? formatValue(r.unit, g.green) : '—'}</td>
      <td className="end">{g ? formatValue(r.unit, g.superGreen) : '—'}</td>
      <td>{g?.responsable ?? '—'}</td>
      <td>{g ? shortDay(g.validFrom) : <span className="badge">Sin meta</span>}</td>
      <td className="end">
        {canEdit && (
          <button type="button" className="small" onClick={onEdit}>
            {g ? 'Cambiar' : 'Poner meta'}
          </button>
        )}
      </td>
    </tr>
  );
}

const text = (n?: number | null) => (n == null ? '' : String(n));
const optional = (s: string) => (s.trim() === '' ? null : Number(s));

function MetaForm({
  row: r,
  rutaId: initialRuta,
  responsables,
  onClose,
}: {
  row: KpiGoalRow;
  rutaId: number | null;
  responsables: KpiGoals['responsables'];
  onClose: () => void;
}) {
  const today = todayInMexicoCity();
  const [rutaId, setRutaId] = useState<number | null>(initialRuta);
  const ruta = r.rutas.find((x) => x.rutaId === rutaId);
  const base = rutaId == null ? (r.next ?? r.current) : (ruta?.next ?? ruta?.current ?? null);
  const periods = r.scaling === 'SamePeriod' ? GOAL_PERIODS.filter((p) => p.key !== 'Dia') : GOAL_PERIODS;
  const [period, setPeriod] = useState<KpiGoalPeriod>(periods.find((p) => p.key === base?.period)?.key ?? 'Semana');
  const [red, setRed] = useState(text(base?.red));
  const [green, setGreen] = useState(text(base?.green));
  const [superGreen, setSuperGreen] = useState(text(base?.superGreen));
  const [responsable, setResponsable] = useState(base?.responsableUserId ? String(base.responsableUserId) : '');
  const [validFrom, setValidFrom] = useState(today);
  const save = useSave(
    () =>
      kpisApi.saveGoal(r.key, {
        period,
        red: optional(red),
        green: Number(green),
        superGreen: optional(superGreen),
        responsableUserId: responsable ? Number(responsable) : null,
        validFrom,
        rutaId,
      }),
    { refetch: [metasKey, kpisKey], message: 'Meta guardada.', onDone: onClose },
  );
  const remove = useAction((g: KpiGoal) => kpisApi.removeGoal(r.key, g.id), [metasKey, kpisKey], 'Meta programada quitada.');
  const order = r.higherIsBetter ? 'rojo < verde < súper verde' : 'rojo > verde > súper verde';
  const step = r.unit === 'Pesos' ? '1' : '0.01';
  const history = r.history.filter((g) => g.rutaId === rutaId);

  return (
    <Dialog title={`Meta · ${r.name}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <p className="muted" style={{ marginTop: 0 }}>
        {r.calculation} {r.higherIsBetter ? 'Más es mejor' : 'Menos es mejor'}: {order}.
      </p>
      <div className="form-grid">
        <Field label="Aplica a" hint="Sin meta de empresa, el total usa la suma de las metas por ruta.">
          <select value={rutaId ?? ''} onChange={(e) => setRutaId(e.target.value ? Number(e.target.value) : null)}>
            <option value="">Toda la empresa</option>
            {r.rutas.map((x) => (
              <option key={x.rutaId} value={x.rutaId}>
                Ruta {x.code}
              </option>
            ))}
          </select>
        </Field>
        {r.scaling !== 'None' ? (
          <Field
            label="La meta es"
            hint={
              r.scaling === 'ByDays'
                ? 'Al ver otro periodo, la meta se ajusta a sus días.'
                : 'Cuenta invitados distintos: la meta se compara solo en periodos de su tipo.'
            }
          >
            <select value={period} onChange={(e) => setPeriod(e.target.value as KpiGoalPeriod)}>
              {periods.map((p) => (
                <option key={p.key} value={p.key}>
                  {p.name}
                </option>
              ))}
            </select>
          </Field>
        ) : (
          <Field label="Periodo" hint="Es un promedio o un porcentaje: la meta vale igual por semana, mes o trimestre.">
            <input value="Cualquiera" disabled />
          </Field>
        )}
        <Field label="Rojo" hint="Opcional.">
          <input type="number" step={step} value={red} onChange={(e) => setRed(e.target.value)} />
        </Field>
        <Field label="Verde">
          <input type="number" step={step} value={green} onChange={(e) => setGreen(e.target.value)} required />
        </Field>
        <Field label="Súper verde" hint="Opcional: sin él nunca se pinta súper verde.">
          <input type="number" step={step} value={superGreen} onChange={(e) => setSuperGreen(e.target.value)} />
        </Field>
        <Field label="Responsable">
          <select value={responsable} onChange={(e) => setResponsable(e.target.value)}>
            <option value="">Sin responsable</option>
            {responsables.map((u) => (
              <option key={u.id} value={u.id}>
                {u.name} ({u.roleName})
              </option>
            ))}
          </select>
        </Field>
        <Field label="Aplica desde" hint="Hoy o después; lo anterior conserva su meta.">
          <input type="date" min={today} value={validFrom} onChange={(e) => setValidFrom(e.target.value)} required />
        </Field>
      </div>
      {history.length > 0 && (
        <div className="field wide" style={{ marginTop: 16 }}>
          <span>Historial de la meta</span>
          <ul className="kpi-goal-history">
            {history.map((g) => (
              <li key={g.id}>
                Desde el {shortDay(g.validFrom)}: {g.red != null && `rojo ${formatValue(r.unit, g.red)} · `}verde {formatValue(r.unit, g.green)}
                {g.superGreen != null && ` · súper verde ${formatValue(r.unit, g.superGreen)}`}
                {r.scaling !== 'None' && ` (${goalPeriodName(g.period).toLowerCase()})`}
                {g.responsable && ` · ${g.responsable}`}
                {g.createdBy && <span className="muted"> · capturó {g.createdBy}</span>}
                {g.validFrom > today && (
                  <button type="button" className="small link" disabled={remove.isPending} onClick={() => remove.mutate(g)}>
                    Quitar
                  </button>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </Dialog>
  );
}

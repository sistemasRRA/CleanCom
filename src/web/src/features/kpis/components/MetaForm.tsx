import { useState } from 'react';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { useAction, useSave } from '../../../shared/hooks/useSave';
import { shortDay, todayInMexicoCity } from '../../../shared/utils/dates';
import { kpisApi } from '../services/kpisApi';
import { kpisKey, metasKey } from '../services/queryKeys';
import { formatValue, GOAL_PERIODS, goalPeriodName, type KpiGoal, type KpiGoalPeriod, type KpiGoalRow, type KpiGoals } from '../types/kpis';

const text = (n?: number | null) => (n == null ? '' : String(n));
const optional = (s: string) => (s.trim() === '' ? null : Number(s));

/**
 * The full form of a goal ("Más" in Metas): everything of the row plus a start date later than today, the history of the
 * goal and removing one that has not started yet.
 */
export function MetaForm({
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

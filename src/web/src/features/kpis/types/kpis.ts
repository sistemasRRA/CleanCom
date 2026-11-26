import type { Schemas } from '../../../shared/api/client';

export type KpiBoard = Schemas['KpiBoardDto'];
export type KpiCard = Schemas['KpiCardDto'];
export type KpiDetail = Schemas['KpiDetailDto'];
export type KpiHistory = Schemas['KpiHistoryDto'];
export type KpiGoals = Schemas['KpiGoalsDto'];
export type KpiGoalRow = Schemas['KpiGoalRowDto'];
export type KpiGoal = Schemas['KpiGoalDto'];
export type KpiPeriod = Schemas['KpiPeriod'];
export type KpiLight = Schemas['KpiLight'];
export type KpiUnit = Schemas['KpiUnit'];
export type KpiArea = Schemas['KpiArea'];
export type KpiGoalPeriod = NonNullable<Schemas['KpiGoalPeriod']>;

export const PERIODS: { key: KpiPeriod; name: string }[] = [
  { key: 'Semana', name: 'Semana' },
  { key: 'Mes', name: 'Mes' },
  { key: 'Trimestre', name: 'Trimestre' },
];

export const AREAS: { key: KpiArea; name: string }[] = [
  { key: 'Logistica', name: 'Logística' },
  { key: 'Venta', name: 'Venta' },
  { key: 'Experiencia', name: 'Experiencia' },
];

export const LIGHTS: { key: KpiLight; name: string }[] = [
  { key: 'SuperVerde', name: 'Súper verde' },
  { key: 'Verde', name: 'Verde' },
  { key: 'Rojo', name: 'Rojo' },
  { key: 'SinMeta', name: 'Sin meta' },
];

export const lightName = (light: KpiLight) => LIGHTS.find((l) => l.key === light)?.name ?? light;

export const GOAL_PERIODS: { key: KpiGoalPeriod; name: string }[] = [
  { key: 'Dia', name: 'Al día' },
  { key: 'Semana', name: 'A la semana' },
  { key: 'Mes', name: 'Al mes' },
];

export const goalPeriodName = (p: KpiGoalPeriod | null) => GOAL_PERIODS.find((g) => g.key === p)?.name ?? '';

const number = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 2 });
const pesos = new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 });

/** A value in its unit: "8.5", "75%", "$12,300", "40"; "—" without data. */
export function formatValue(unit: KpiUnit, value?: number | null) {
  if (value == null) return '—';
  switch (unit) {
    case 'Porcentaje':
      return `${number.format(value)}%`;
    case 'Pesos':
      return pesos.format(value);
    default:
      return number.format(value);
  }
}

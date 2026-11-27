import type { Schemas } from '../../../shared/api/client';

export type Period = Schemas['ClientesPeriod'];
export type Tipo = Schemas['ClientesTipo'];
export type Totals = Schemas['ClientesTotalsDto'];
export type Perdido = Schemas['PerdidoDto'];

export const PERIODS: { key: Period; name: string }[] = [
  { key: 'Semana', name: 'Semana' },
  { key: 'Mes', name: 'Mes' },
  { key: 'Trimestre', name: 'Trimestre' },
  { key: 'Anio', name: 'Año' },
];

export const TIPOS: { key: Tipo; name: string }[] = [
  { key: 'Todos', name: 'Todos' },
  { key: 'ClientesFinales', name: 'Clientes finales' },
  { key: 'Convenios', name: 'Convenios y empresas' },
];

export { money } from '../../../shared/utils/format';

export const percent = (n?: number | null) => (n == null ? '—' : `${n.toLocaleString('es-MX')} %`);

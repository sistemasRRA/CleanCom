import type { Schemas } from '../../../shared/api/client';

export type Pedido = Schemas['PedidoDto'];

export type Board = Schemas['AgendaBoardDto'];

export type Ruta = Schemas['BoardRutaDto'];

type Turno = Schemas['Turno'];

export type View = 'semana' | 'dia';

/** Rows of the day view, in the order the office reads them. */
export const TURNOS: { key: Turno; name: string; hint: string }[] = [
  { key: 'Manana', name: 'Mañana', hint: '9:00 a 13:00' },
  { key: 'Tarde', name: 'Tarde', hint: '13:00 a 18:00' },
  { key: 'HoraEspecifica', name: 'Hora específica', hint: 'Solo oficina' },
  { key: 'CualquierHora', name: 'Cualquier hora', hint: 'Toda la jornada' },
];

const TURNO_ORDER: Record<Turno, number> = { Manana: 0, Tarde: 1, HoraEspecifica: 2, CualquierHora: 3 };

// ---- Business dates (yyyy-MM-dd) without time zone shifts ----
export const toDate = (iso: string) => new Date(`${iso}T00:00:00Z`);

const toIso = (d: Date) => d.toISOString().slice(0, 10);

export const addDays = (iso: string, n: number) => {
  const d = toDate(iso);
  d.setUTCDate(d.getUTCDate() + n);
  return toIso(d);
};

export const mondayOf = (iso: string) => addDays(iso, -((toDate(iso).getUTCDay() + 6) % 7));

export const monthTitle = new Intl.DateTimeFormat('es-MX', { month: 'long', year: 'numeric', timeZone: 'UTC' });

export const weekday = new Intl.DateTimeFormat('es-MX', { weekday: 'short', timeZone: 'UTC' });

export function sortPedidos(list: Pedido[]) {
  return [...list].sort(
    (a, b) =>
      (a.rutaCode ?? '').localeCompare(b.rutaCode ?? '') ||
      TURNO_ORDER[a.turno] - TURNO_ORDER[b.turno] ||
      (a.specificTime ?? '').localeCompare(b.specificTime ?? '') ||
      a.invitadoName.localeCompare(b.invitadoName),
  );
}

export const turnoShort: Record<Turno, string> = { Manana: 'Mañana', Tarde: 'Tarde', HoraEspecifica: '', CualquierHora: 'Cualquier hora' };

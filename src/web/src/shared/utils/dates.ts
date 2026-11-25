export type Weekday = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';

/** Monday first, with the Spanish names shown on screen. */
export const WEEKDAYS: { key: Weekday; name: string }[] = [
  { key: 'Monday', name: 'Lunes' },
  { key: 'Tuesday', name: 'Martes' },
  { key: 'Wednesday', name: 'Miércoles' },
  { key: 'Thursday', name: 'Jueves' },
  { key: 'Friday', name: 'Viernes' },
  { key: 'Saturday', name: 'Sábado' },
  { key: 'Sunday', name: 'Domingo' },
];

const dateTimeFormat = new Intl.DateTimeFormat('es-MX', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'America/Mexico_City',
});

const dateFormat = new Intl.DateTimeFormat('es-MX', { dateStyle: 'full', timeZone: 'UTC' });

/** UTC instant from the API shown in Mexico City time. */
export function formatDateTime(iso?: string | null) {
  return iso ? dateTimeFormat.format(new Date(iso)) : '—';
}

/** Business date (yyyy-MM-dd) shown without time zone conversion. */
export function formatDate(iso: string) {
  return dateFormat.format(new Date(`${iso}T00:00:00Z`));
}

/** Today in Mexico City (yyyy-MM-dd): business dates are in CDMX time. */
export const todayInMexicoCity = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(new Date());

/** Adds days to a business date (yyyy-MM-dd). */
export function addDays(iso: string, days: number) {
  const d = new Date(`${iso}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

const shortDayFormat = new Intl.DateTimeFormat('es-MX', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
/** "sáb 21 nov" for a business date (yyyy-MM-dd); "—" when there is none. */
export const shortDay = (iso?: string | null) => (iso ? shortDayFormat.format(new Date(`${iso}T00:00:00Z`)).replace('.', '') : '—');

/** Time of an instant in Mexico City ("09:30 a.m."). */
export const clock = (iso?: string | null) =>
  iso ? new Date(iso).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' }) : '';

/** "09:30" from a time of day ("09:30:00"). */
export const hm = (t?: string | null) => (t ? t.slice(0, 5) : '');

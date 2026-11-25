import type { Schemas } from '../../../shared/api/client';
import { type Weekday, WEEKDAYS } from '../../../shared/utils/dates';
import { optional } from '../../../shared/utils/format';

export type Fija = Schemas['ParadaFijaDto'];

export type Kind = Schemas['ParadaKind'];

export type Category = NonNullable<Schemas['ParadaCategory']>;

export type Turno = Schemas['Turno'];

export type Placement = Schemas['ParadaPlacement'];

export type Lugar = Schemas['LugarDto'];

/** Tipo de parada, with the hint and the color class used on screen. */
export const CATEGORIES: { key: Category; name: string; hint?: string; tone: string }[] = [
  { key: 'ClienteFinal', name: 'Cliente final', tone: 'kind-cliente' },
  { key: 'Convenio', name: 'Convenio', hint: 'Empresa o cliente con convenio y visita fija', tone: 'kind-convenio' },
  { key: 'Insumos', name: 'Insumos', tone: 'kind-insumos' },
  { key: 'Traspaso', name: 'Traspaso', hint: 'Movimiento de ropa entre tiendas 7clean o a Leones', tone: 'kind-traspaso' },
];

/** The schema marks the enum nullable (the chofer's paradas may have none); a parada fija always has one. */
export const categoryOf = (c: Category | null) => CATEGORIES.find((x) => x.key === c) ?? CATEGORIES[0];

/** "Qué se hace". */
export const ACTIONS: { key: Kind; name: string }[] = [
  { key: 'Recoleccion', name: 'Recolección' },
  { key: 'Entrega', name: 'Entrega' },
  { key: 'RecoleccionYEntrega', name: 'Entrega y recolección' },
  { key: 'Traslado', name: 'Traslado' },
];

export const actionName = (k: Kind) => ACTIONS.find((x) => x.key === k)?.name ?? k;

export const TURNOS: { key: Turno; name: string }[] = [
  { key: 'Manana', name: 'Por la mañana' },
  { key: 'Tarde', name: 'Por la tarde' },
  { key: 'CualquierHora', name: 'Cualquier hora' },
];

export const turnoName = (t: Turno) => TURNOS.find((x) => x.key === t)?.name ?? t;

export const PLACEMENTS: { key: Placement; name: string; short: string }[] = [
  { key: 'Automatica', name: 'Automático (donde quede mejor dentro del turno)', short: '' },
  { key: 'Primera', name: 'Primera parada del día', short: '1ª' },
  { key: 'Ultima', name: 'Última parada antes de Leones', short: 'Últ.' },
];

export const placementName = (p: Placement) => PLACEMENTS.find((x) => x.key === p)?.name.split(' (')[0] ?? p;

export const dayShort = (d: Weekday) => WEEKDAYS.find((x) => x.key === d)?.name.slice(0, 2) ?? d;

const shortDate = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short', timeZone: 'UTC' });

export const short = (iso: string) => shortDate.format(new Date(`${iso}T00:00:00Z`)).replace('.', '');

/** kind and turno '' = same as the parada fija. */
export type PasoForm = {
  lugarId: string;
  address: string;
  mapsLink: string;
  kind: Kind | '';
  turno: Turno | '';
  before: string;
  activity: string;
  notes: string;
};

export const emptyPaso: PasoForm = { lugarId: '', address: '', mapsLink: '', kind: '', turno: '', before: '', activity: '', notes: '' };

/** What the API receives for a captured paso. */
export function toPasoRequest(p: PasoForm) {
  return {
    lugarId: p.lugarId ? Number(p.lugarId) : null,
    address: p.lugarId ? null : optional(p.address),
    lat: null,
    lng: null,
    mapsLink: p.lugarId ? null : optional(p.mapsLink),
    kind: p.kind || null,
    turno: p.turno || null,
    before: p.before ? `${p.before}:00` : null,
    activity: optional(p.activity),
    notes: optional(p.notes),
  };
}

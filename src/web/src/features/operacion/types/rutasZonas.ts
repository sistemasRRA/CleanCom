import type { Schemas } from '../../../shared/api/client';
import type { Point } from '../../../shared/components/LocationMap';
import type { MapZona } from '../components/PolygonMap';

export type Ruta = Schemas['RutaDto'];

export type Zona = Schemas['ZonaDto'];

/** "13:00:00" → "13:00". */
export function shortTime(value?: string | null) {
  return value ? value.slice(0, 5) : null;
}

/** Vertices with both numbers (half-typed lines are skipped on the map). */
export const toPoints = (list: Schemas['Vertex'][]): Point[] => list.filter((v): v is Point => Number.isFinite(v.lat) && Number.isFinite(v.lng));

/** The zonas with a polygon, in the color of their ruta, for the map. */
export function mapZonas(zonas: Zona[], rutas: Ruta[]): MapZona[] {
  return zonas
    .filter((z) => z.active && (z.polygon?.length ?? 0) >= 3)
    .map((z) => ({ id: z.id, code: z.code, color: rutas.find((r) => r.id === z.rutaId)?.color ?? '#64748b', polygon: toPoints(z.polygon!) }));
}

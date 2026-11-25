import L from 'leaflet';
import { useEffect, useRef } from 'react';
import 'leaflet/dist/leaflet.css';
import type { Point } from '../../../shared/components/LocationMap';

/** Zócalo of CDMX: where the map starts when there is nothing drawn yet. */
const CDMX: Point = { lat: 19.4326, lng: -99.1332 };

export type MapZona = { id: number; code: string; color: string; polygon: Point[] };

const vertexIcon = L.divIcon({ className: 'polygon-vertex', iconSize: [14, 14], iconAnchor: [7, 7] });

const middleIcon = L.divIcon({ className: 'polygon-vertex middle', iconSize: [10, 10], iconAnchor: [5, 5] });

const round = (p: L.LatLng): Point => ({ lat: Number(p.lat.toFixed(6)), lng: Number(p.lng.toFixed(6)) });

/**
 * OpenStreetMap map with the zonas' polygons. With `onChange` it edits `value`: a click on the map adds a vertex at
 * the end, a vertex is dragged to move it or clicked to remove it, and the small dot between two vertices is dragged
 * (or clicked) to add one in between. The other zonas are drawn faintly behind, with their code, as reference.
 */
export function PolygonMap({
  value = [],
  color = '#1e2a78',
  others,
  onChange,
  className = 'polygon-map',
}: {
  value?: Point[];
  color?: string;
  others: MapZona[];
  onChange?: (polygon: Point[]) => void;
  className?: string;
}) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const othersLayer = useRef<L.LayerGroup | null>(null);
  const editLayer = useRef<L.LayerGroup | null>(null);
  const fitted = useRef(false);
  const frame = useRef<() => void>(() => {});
  const latest = useRef({ value, onChange });
  latest.current = { value, onChange };

  useEffect(() => {
    const m = L.map(container.current!, { center: [CDMX.lat, CDMX.lng], zoom: 11, doubleClickZoom: false });
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">OpenStreetMap</a>',
    }).addTo(m);
    othersLayer.current = L.layerGroup().addTo(m);
    editLayer.current = L.layerGroup().addTo(m);
    m.on('click', (e: L.LeafletMouseEvent) => {
      const { value: v, onChange: change } = latest.current;
      change?.([...v, round(e.latlng)]);
    });
    map.current = m;

    // In a dialog the map is created before it is shown (size 0): recompute when its box changes, and frame it then.
    const resize = new ResizeObserver(() => {
      m.invalidateSize();
      frame.current();
    });
    resize.observe(container.current!);
    return () => {
      resize.disconnect();
      m.remove();
      map.current = null;
      fitted.current = false;
    };
  }, []);

  // The other zonas, behind and not clickable (a click there still adds a vertex).
  useEffect(() => {
    const layer = othersLayer.current;
    if (!layer) return;
    layer.clearLayers();
    for (const z of others) {
      if (z.polygon.length < 3) continue;
      L.polygon(
        z.polygon.map((p) => [p.lat, p.lng] as L.LatLngTuple),
        {
          color: z.color,
          weight: onChange ? 1.5 : 2,
          fillOpacity: onChange ? 0.08 : 0.18,
          interactive: false,
        },
      )
        .bindTooltip(z.code, { permanent: true, direction: 'center', className: 'polygon-label' })
        .addTo(layer);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [JSON.stringify(others)]);

  // The polygon being edited, with its handles.
  useEffect(() => {
    const layer = editLayer.current;
    if (!layer) return;
    layer.clearLayers();
    const points = value.map((p) => L.latLng(p.lat, p.lng));
    const shape = (
      points.length >= 3
        ? L.polygon(points, { color, weight: 3, fillOpacity: 0.2, interactive: false })
        : L.polyline(points, { color, weight: 3, dashArray: '6 6', interactive: false })
    ).addTo(layer);
    if (!onChange) return;

    points.forEach((point, i) => {
      const marker = L.marker(point, { icon: vertexIcon, draggable: true, title: 'Arrastra para mover · clic para quitar' }).addTo(layer);
      marker.on('drag', () => shape.setLatLngs(points.map((p, j) => (j === i ? marker.getLatLng() : p))));
      marker.on('dragend', () => onChange(value.map((p, j) => (j === i ? round(marker.getLatLng()) : p))));
      marker.on('click', () => onChange(value.filter((_, j) => j !== i)));
    });
    // A dot halfway along each side (the closing one too) adds a vertex there.
    if (points.length >= 3) {
      points.forEach((point, i) => {
        const nextPoint = points[(i + 1) % points.length];
        const middle = L.latLng((point.lat + nextPoint.lat) / 2, (point.lng + nextPoint.lng) / 2);
        const marker = L.marker(middle, { icon: middleIcon, draggable: true, title: 'Arrastra para agregar un vértice' }).addTo(layer);
        const insert = (p: L.LatLng) => onChange([...value.slice(0, i + 1), round(p), ...value.slice(i + 1)]);
        marker.on('drag', () => shape.setLatLngs([...points.slice(0, i + 1), marker.getLatLng(), ...points.slice(i + 1)]));
        marker.on('dragend', () => insert(marker.getLatLng()));
        marker.on('click', () => insert(middle));
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [JSON.stringify(value), color, !!onChange]);

  // Frame once, when the map has a size: the zona's own polygon, otherwise all the zonas, otherwise CDMX.
  frame.current = () => {
    const m = map.current;
    if (!m || fitted.current) return;
    // Leaflet keeps the size it measured when created; measure again (a dialog or the styles may have arrived since).
    m.invalidateSize();
    const size = m.getSize();
    if (size.x === 0 || size.y === 0) return;
    const own = value.map((p) => [p.lat, p.lng] as L.LatLngTuple);
    const all = others.flatMap((z) => z.polygon.map((p) => [p.lat, p.lng] as L.LatLngTuple));
    const target = own.length > 0 ? own : all;
    if (target.length === 0) return;
    fitted.current = true;
    m.fitBounds(L.latLngBounds(target), { padding: [24, 24], maxZoom: 15 });
  };
  useEffect(() => frame.current());

  return <div ref={container} className={className} />;
}

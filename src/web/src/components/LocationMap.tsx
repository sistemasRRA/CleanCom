import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

export type Point = { lat: number; lng: number };

/** Zócalo of CDMX: where the map starts when the address has no point yet. */
const CDMX: Point = { lat: 19.4326, lng: -99.1332 };

/** A plain CSS pin: Leaflet's default icon images don't survive the bundler. */
const pin = L.divIcon({ className: 'location-pin', html: '<span></span>', iconSize: [26, 26], iconAnchor: [13, 26] });

/**
 * Small OpenStreetMap map with a pin the user drags (or clicks the map) to the exact house.
 * A point that comes from outside (typed or found from the address) recenters the map; one the user placed does not.
 */
export function LocationMap({ point, onChange }: { point: Point | null; onChange: (p: Point) => void }) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const marker = useRef<L.Marker | null>(null);
  const placedHere = useRef<Point | null>(null);
  const onChangeRef = useRef(onChange);
  onChangeRef.current = onChange;

  useEffect(() => {
    const m = L.map(container.current!, { center: [CDMX.lat, CDMX.lng], zoom: 11 });
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">OpenStreetMap</a>',
    }).addTo(m);

    function place(p: Point) {
      const rounded = { lat: Number(p.lat.toFixed(6)), lng: Number(p.lng.toFixed(6)) };
      placedHere.current = rounded;
      onChangeRef.current(rounded);
    }
    m.on('click', (e: L.LeafletMouseEvent) => place(e.latlng));
    map.current = m;

    // In a dialog the map is created before it is shown (size 0): recompute when its box changes.
    const resize = new ResizeObserver(() => m.invalidateSize());
    resize.observe(container.current!);
    return () => {
      resize.disconnect();
      m.remove();
      map.current = null;
      marker.current = null;
    };
  }, []);

  useEffect(() => {
    const m = map.current;
    if (!m) return;
    if (!point) {
      marker.current?.remove();
      marker.current = null;
      return;
    }
    if (!marker.current) {
      marker.current = L.marker([point.lat, point.lng], { icon: pin, draggable: true, autoPan: true }).addTo(m);
      marker.current.on('dragend', () => {
        const p = marker.current!.getLatLng();
        const rounded = { lat: Number(p.lat.toFixed(6)), lng: Number(p.lng.toFixed(6)) };
        placedHere.current = rounded;
        onChangeRef.current(rounded);
      });
    } else {
      marker.current.setLatLng([point.lat, point.lng]);
    }
    const mine = placedHere.current && placedHere.current.lat === point.lat && placedHere.current.lng === point.lng;
    if (!mine) m.setView([point.lat, point.lng], Math.max(m.getZoom(), 17));
    // By value: the parent parses a new object on every render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [point?.lat, point?.lng]);

  return <div ref={container} className="location-map" />;
}

import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { STATE, type Vista } from '../types/chofer';
import { isOld, type LiveLocation, useNow } from '../../../shared/api/realtime';
import { escapeHtml } from '../../../shared/utils/format';
import { clock } from '../../../shared/utils/dates';

/** The ruta's paradas numbered in the chofer's order, joined in that order, and the chofer now. */
export function ChoferMap({ vista, live }: { vista: Vista; live: LiveLocation | null }) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const layer = useRef<L.LayerGroup | null>(null);
  const lastFit = useRef('');
  const now = useNow();

  useEffect(() => {
    const m = L.map(container.current!, { center: [19.4, -99.17], zoom: 12, scrollWheelZoom: false });
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">OpenStreetMap</a>',
    }).addTo(m);
    m.on('click', () => m.scrollWheelZoom.enable());
    m.on('mouseout', () => m.scrollWheelZoom.disable());
    layer.current = L.layerGroup().addTo(m);
    map.current = m;
    const resize = new ResizeObserver(() => m.invalidateSize());
    resize.observe(container.current!);
    return () => {
      resize.disconnect();
      m.remove();
    };
  }, []);

  useEffect(() => {
    const m = map.current;
    const g = layer.current;
    if (!m || !g) return;
    g.clearLayers();
    const color = vista.ruta?.color ?? '#1e2a78';
    const stops = vista.paradas.filter((p) => p.lat != null && p.lng != null);
    const points: L.LatLngTuple[] = stops.map((p) => [p.lat!, p.lng!]);
    if (points.length > 1) L.polyline(points, { color, weight: 3, opacity: 0.7 }).addTo(g);
    for (const p of stops) {
      const done = p.state === 'Realizada' || p.state === 'NoRealizada';
      const icon = L.divIcon({
        className: 'routes-map__stop',
        html: `<span style="background:${escapeHtml(color)}" class="${done ? 'done' : ''}">${p.order}</span>`,
        iconSize: [26, 26],
        iconAnchor: [13, 13],
      });
      L.marker([p.lat!, p.lng!], { icon })
        .bindTooltip(`<strong>${p.order}. ${escapeHtml(p.invitadoName ?? 'Parada')}</strong><br>${escapeHtml(STATE[p.state].label)}`, {
          direction: 'top',
          offset: [0, -12],
        })
        .addTo(g);
    }
    if (live) {
      const old = isOld(live, now);
      const icon = L.divIcon({
        className: `routes-map__live${old ? ' stale' : ''}`,
        html: `<span style="background:${escapeHtml(color)}">${escapeHtml(live.rutaCode)}</span>`,
        iconSize: [34, 34],
        iconAnchor: [17, 17],
      });
      L.marker([live.lat, live.lng], { icon, zIndexOffset: 2000 })
        .bindTooltip(`${escapeHtml(live.operadorName ?? 'Chofer')} · ${old ? 'sin señal desde las ' : ''}${clock(live.at)}`, {
          direction: 'top',
          offset: [0, -16],
        })
        .addTo(g);
    }
    // Fit only when the ruta or the day changes, not on every position.
    const fitKey = `${vista.ruta?.id}|${vista.date}|${points.length}`;
    const all = live ? [...points, [live.lat, live.lng] as L.LatLngTuple] : points;
    if (fitKey !== lastFit.current && all.length > 0) {
      lastFit.current = fitKey;
      m.fitBounds(L.latLngBounds(all).pad(0.15), { maxZoom: 15 });
    }
  }, [vista, live, now]);

  return <div ref={container} className="routes-map__canvas vista-map__canvas" />;
}

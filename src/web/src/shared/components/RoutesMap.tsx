import { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { Schemas } from '../api/client';
import { isOld, type LiveLocation, useNow } from '../api/realtime';
import { escapeHtml } from '../utils/format';
import { hm } from '../utils/dates';

type Plan = Schemas['RutaPlanDto'];

/**
 * Map of the day's rutas: the base (B) and each ruta's paradas numbered in visit order with the ruta's color,
 * joined in that order from and back to the base. Lines are straight (no streets) until Google Routes is connected.
 * `live`: where each chofer is now (hub "operacion"), drawn over the plan with the ruta's color.
 */
export function RoutesMap({ base, rutas, live = [] }: { base: { lat: number; lng: number }; rutas: Plan[]; live?: LiveLocation[] }) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const layer = useRef<L.LayerGroup | null>(null);
  const liveLayer = useRef<L.LayerGroup | null>(null);
  const [hidden, setHidden] = useState<Set<number>>(new Set());
  const now = useNow();

  useEffect(() => {
    const m = L.map(container.current!, { center: [base.lat, base.lng], zoom: 12, scrollWheelZoom: false });
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">OpenStreetMap</a>',
    }).addTo(m);
    // Scroll zoom only after a click on the map, so scrolling the page does not zoom it by accident.
    m.on('click', () => m.scrollWheelZoom.enable());
    m.on('mouseout', () => m.scrollWheelZoom.disable());
    layer.current = L.layerGroup().addTo(m);
    liveLayer.current = L.layerGroup().addTo(m);
    map.current = m;
    const resize = new ResizeObserver(() => m.invalidateSize());
    resize.observe(container.current!);
    return () => {
      resize.disconnect();
      m.remove();
      map.current = null;
    };
    // The map is created once; the base only centers it.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Redraw when the plan or the visible rutas change; fit the view only when the day or the rutas shown change.
  const shownKey = rutas.map((r) => `${r.rutaId}:${hidden.has(r.rutaId) ? 0 : 1}`).join(',');
  const lastFit = useRef('');
  useEffect(() => {
    const m = map.current;
    const g = layer.current;
    if (!m || !g) return;
    g.clearLayers();
    const bounds = L.latLngBounds([[base.lat, base.lng]]);

    for (const r of rutas) {
      if (hidden.has(r.rutaId)) continue;
      const stops = r.paradas.filter((p) => p.lat != null && p.lng != null);
      const path: L.LatLngExpression[] = [[base.lat, base.lng], ...stops.map((p) => [p.lat!, p.lng!] as L.LatLngTuple), [base.lat, base.lng]];
      if (stops.length > 0) {
        L.polyline(path, { color: r.color, weight: 3, opacity: 0.75 }).addTo(g);
      }
      for (const p of stops) {
        const done = p.state === 'Realizada' || p.state === 'NoRealizada';
        const icon = L.divIcon({
          className: 'routes-map__stop',
          html: `<span style="background:${escapeHtml(r.color)}" class="${done ? 'done' : ''}">${p.order}</span>`,
          iconSize: [26, 26],
          iconAnchor: [13, 13],
        });
        L.marker([p.lat!, p.lng!], { icon, title: `${r.code} · ${p.order}` })
          .bindTooltip(
            `<strong>${escapeHtml(r.code)} · ${p.order}. ${escapeHtml(p.name ?? 'Parada')}</strong><br>${escapeHtml(p.address ?? '')}<br>Llega ${hm(p.arrival)} · ventana ${hm(p.windowStart)} a ${hm(p.windowEnd)}`,
            { direction: 'top', offset: [0, -12] },
          )
          .addTo(g);
        bounds.extend([p.lat!, p.lng!]);
      }
    }

    const baseIcon = L.divIcon({ className: 'routes-map__base', html: '<span>B</span>', iconSize: [30, 30], iconAnchor: [15, 15] });
    L.marker([base.lat, base.lng], { icon: baseIcon, zIndexOffset: 1000, title: 'Base (Leones)' })
      .bindTooltip('<strong>Base (Leones)</strong><br>Salida y regreso de las rutas', { direction: 'top', offset: [0, -14] })
      .addTo(g);

    const fitKey = `${base.lat},${base.lng}|${shownKey}|${rutas.map((r) => r.paradas.length).join(',')}`;
    if (fitKey !== lastFit.current) {
      lastFit.current = fitKey;
      m.fitBounds(bounds.pad(0.1), { maxZoom: 15 });
    }
  }, [rutas, hidden, base.lat, base.lng, shownKey]);

  // Choferes now: redrawn on every reading, without moving the view.
  useEffect(() => {
    const g = liveLayer.current;
    if (!g) return;
    g.clearLayers();
    for (const l of live) {
      const ruta = rutas.find((r) => r.rutaId === l.rutaId);
      if (ruta && hidden.has(ruta.rutaId)) continue;
      const color = ruta?.color ?? l.rutaColor ?? '#1e2a78';
      const at = new Date(l.at).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' });
      const old = isOld(l, now);
      const icon = L.divIcon({
        className: `routes-map__live${old ? ' stale' : ''}`,
        html: `<span style="background:${escapeHtml(color)}">${escapeHtml(l.rutaCode)}</span>`,
        iconSize: [34, 34],
        iconAnchor: [17, 17],
      });
      L.marker([l.lat, l.lng], { icon, zIndexOffset: 2000, title: `Ruta ${l.rutaCode}` })
        .bindTooltip(
          `<strong>Ruta ${escapeHtml(l.rutaCode)} · ${escapeHtml(l.operadorName ?? 'Chofer')}</strong><br>${!l.live ? 'Última ubicación guardada' : old ? 'Sin señal; última ubicación' : 'Ubicación'} de las ${at}`,
          { direction: 'top', offset: [0, -16] },
        )
        .addTo(g);
    }
  }, [live, rutas, hidden, now]);

  const withoutLocation = rutas
    .filter((r) => !hidden.has(r.rutaId))
    .reduce((n, r) => n + r.paradas.filter((p) => p.lat == null || p.lng == null).length, 0);

  return (
    <div className="routes-map">
      <div className="routes-map__legend">
        {rutas.map((r) => {
          const on = !hidden.has(r.rutaId);
          return (
            <button
              key={r.rutaId}
              type="button"
              className={`small${on ? ' selected' : ''}`}
              onClick={() =>
                setHidden((h) => {
                  const next = new Set(h);
                  if (on) next.add(r.rutaId);
                  else next.delete(r.rutaId);
                  return next;
                })
              }
              title={on ? 'Ocultar del mapa' : 'Mostrar en el mapa'}
            >
              <span className="ruta-color" style={{ background: r.color, opacity: on ? 1 : 0.35 }} />
              Ruta {r.code} · {r.paradas.length}
            </button>
          );
        })}
        {withoutLocation > 0 && (
          <span className="muted">
            {withoutLocation} parada{withoutLocation === 1 ? '' : 's'} sin ubicación no aparece{withoutLocation === 1 ? '' : 'n'}
          </span>
        )}
      </div>
      <div ref={container} className="routes-map__canvas" />
    </div>
  );
}

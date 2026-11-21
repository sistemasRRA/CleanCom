import { useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { api, unwrap, type Schemas } from '../../api/client';
import { useRutas } from '../../api/queries';
import { useOperacion, type LiveLocation } from '../../api/realtime';
import { PageHeader, QueryState } from '../../components/Common';

type Vista = Schemas['ChoferRutaDto'];
type Parada = Schemas['ChoferParadaDto'];

const previewKey = ['chofer', 'vista'] as const;

const KIND: Record<Schemas['ParadaKind'], string> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Entrega y recolección',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
  Salida: 'Salida',
  Regreso: 'Regreso',
  Comida: 'Comida',
  Traslado: 'Traslado',
};
const STATE: Record<Schemas['ParadaState'], { label: string; tone: string }> = {
  Pendiente: { label: 'Pendiente', tone: '' },
  EnCamino: { label: 'En camino', tone: 'brand' },
  EnSitio: { label: 'En sitio', tone: 'warning' },
  Realizada: { label: 'Realizada', tone: 'success' },
  NoRealizada: { label: 'No realizada', tone: 'danger' },
};

const hm = (t?: string | null) => (t ? t.slice(0, 5) : '');
const clock = (iso?: string | null) =>
  iso ? new Date(iso).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' }) : '';
const today = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(new Date());
function addDays(iso: string, days: number) {
  const d = new Date(`${iso}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}
const escape = (s: string) => s.replace(/[&<>"']/g, (c) => `&#${c.charCodeAt(0)};`);

/**
 * Vista del chofer (docs/pantallas/oficina/vista-del-chofer.md): a read-only preview of what the chofer of a ruta sees
 * in the app (GET /chofer/vista, the same calculation), with each parada's state and the chofer's position, live.
 */
export function VistaDelChofer() {
  const rutas = useRutas();
  const activas = (rutas.data ?? []).filter((r) => r.active);
  const [rutaId, setRutaId] = useState<number | null>(null);
  const [date, setDate] = useState(today);
  const selected = rutaId ?? activas[0]?.id ?? null;
  const queryClient = useQueryClient();

  const vista = useQuery({
    queryKey: [...previewKey, selected, date],
    enabled: selected != null,
    queryFn: () => unwrap(api.GET('/chofer/vista', { params: { query: { ruta: selected!, fecha: date } } })),
    placeholderData: (previous) => previous,
  });
  const [live, setLive] = useState<LiveLocation | null>(null);
  useEffect(() => setLive(vista.data?.location ?? null), [vista.data]);

  const status = useOperacion({
    onLocation: (l) => {
      if (l.rutaId === selected && l.date === date) setLive(l);
    },
    onParada: (p) => {
      if (p.date === date) void queryClient.invalidateQueries({ queryKey: previewKey });
    },
  });

  const v = vista.data?.ruta;
  return (
    <>
      <PageHeader title="Vista del chofer" description="Lo mismo que ve el chofer en su app, con el estado de cada parada en vivo. Solo lectura.">
        <span className={`badge ${status === 'live' ? 'success' : status === 'offline' ? 'danger' : ''}`}>{status === 'live' ? 'En vivo' : status === 'offline' ? 'Sin conexión en vivo' : 'Conectando…'}</span>
        <Link to="/panel-del-dia" className="button">
          Volver al panel
        </Link>
      </PageHeader>

      <div className="actions vista-toolbar">
        <div className="segmented" role="group" aria-label="Ruta">
          {activas.map((r) => (
            <button key={r.id} type="button" className={`segmented__option${selected === r.id ? ' selected' : ''}`} onClick={() => setRutaId(r.id)}>
              <span className="ruta-color" style={{ background: r.color }} /> Ruta {r.code}
            </button>
          ))}
        </div>
        <button type="button" className="small" onClick={() => setDate((d) => addDays(d, -1))} aria-label="Día anterior">
          ‹
        </button>
        <input type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} aria-label="Fecha" />
        <button type="button" className="small" onClick={() => setDate((d) => addDays(d, 1))} aria-label="Día siguiente">
          ›
        </button>
        {date !== today() && (
          <button type="button" className="small" onClick={() => setDate(today())}>
            Hoy
          </button>
        )}
      </div>

      <QueryState loading={rutas.isLoading || (selected != null && vista.isLoading)} error={rutas.error ?? vista.error}>
        {activas.length === 0 ? (
          <div className="empty">No hay rutas activas.</div>
        ) : (
          v && (
            <div className="vista-chofer">
              <section className="vista-phone" aria-label="Pantalla del chofer">
                <header className="vista-phone__bar">
                  <strong>{v.operadorName ?? 'Sin chofer'}</strong>
                  <span>
                    {v.ruta ? `Ruta ${v.ruta.code}` : 'Sin ruta'} · {v.label}
                  </span>
                </header>
                <div className="vista-phone__body">
                  {v.notice && <div className="alert info">{v.notice}</div>}
                  {v.paradas.length > 0 && (
                    <p className="muted">
                      {v.paradas.length} paradas · {v.paradas.filter((p) => p.state !== 'Realizada' && p.state !== 'NoRealizada').length} pendientes
                      {v.orderRequired ? ' · en el orden asignado' : ''}
                    </p>
                  )}
                  <ol className="vista-paradas">
                    {v.paradas.map((p) => (
                      <ParadaItem key={p.id} parada={p} next={p.id === v.nextParadaId} />
                    ))}
                  </ol>
                </div>
              </section>
              <section className="card vista-map">
                <div className="card__header">
                  <h2>Mapa</h2>
                  <span className="muted">
                    {live ? `${live.live ? 'Ubicación' : 'Última ubicación guardada'} de las ${clock(live.at)}` : 'Sin ubicación del chofer este día.'}
                  </span>
                </div>
                <div className="card__body">
                  <ChoferMap vista={v} live={live} />
                </div>
              </section>
            </div>
          )
        )}
      </QueryState>
    </>
  );
}

function ParadaItem({ parada: p, next }: { parada: Parada; next: boolean }) {
  const state = STATE[p.state];
  const done = p.state === 'Realizada' || p.state === 'NoRealizada';
  return (
    <li className={`vista-parada${next ? ' next' : ''}${done ? ' done' : ''}`}>
      <span className="vista-parada__order">{p.order}</span>
      <div className="vista-parada__body">
        <div className="vista-parada__top">
          <strong>{p.invitadoName ?? 'Parada'}</strong>
          <span className={`badge ${state.tone}`}>{state.label}</span>
          {next && <span className="badge brand">Siguiente</span>}
          {p.priority && <span className="badge danger">Prioridad</span>}
        </div>
        <span className="muted">
          {KIND[p.kind]}
          {p.estimatedArrival && ` · llega ${hm(p.estimatedArrival)}`}
          {p.specificTime && ` · a las ${hm(p.specificTime)}`}
          {p.zonaCode && ` · ${p.zonaCode}`}
        </span>
        {p.address && <span>{p.address}</span>}
        {p.references && <span className="muted">{p.references}</span>}
        {p.activity && <span>{p.activity}</span>}
        {p.viaje && <span className="muted">{p.viaje}</span>}
        {p.notes && <span className="muted">Notas: {p.notes}</span>}
        {(p.enCaminoAt || p.arrivedAt || p.closedAt) && (
          <span className="muted">
            {p.enCaminoAt && `En camino ${clock(p.enCaminoAt)}`}
            {p.arrivedAt && ` · llegó ${clock(p.arrivedAt)}${p.arrivedDistanceMeters != null ? ` a ${p.arrivedDistanceMeters} m` : ''}`}
            {p.arrivedOutsideRadius && ' (fuera de radio)'}
            {p.closedAt && ` · cerrada ${clock(p.closedAt)}`}
            {p.photos > 0 && ` · ${p.photos} foto${p.photos === 1 ? '' : 's'}`}
            {p.signed && ' · firma'}
          </span>
        )}
      </div>
    </li>
  );
}

/** The ruta's paradas numbered in the chofer's order, joined in that order, and the chofer now. */
function ChoferMap({ vista, live }: { vista: Vista; live: LiveLocation | null }) {
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const layer = useRef<L.LayerGroup | null>(null);
  const lastFit = useRef('');

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
        html: `<span style="background:${escape(color)}" class="${done ? 'done' : ''}">${p.order}</span>`,
        iconSize: [26, 26],
        iconAnchor: [13, 13],
      });
      L.marker([p.lat!, p.lng!], { icon })
        .bindTooltip(`<strong>${p.order}. ${escape(p.invitadoName ?? 'Parada')}</strong><br>${escape(STATE[p.state].label)}`, { direction: 'top', offset: [0, -12] })
        .addTo(g);
    }
    if (live) {
      const icon = L.divIcon({
        className: `routes-map__live${live.live ? '' : ' stale'}`,
        html: `<span style="background:${escape(color)}">${escape(live.rutaCode)}</span>`,
        iconSize: [34, 34],
        iconAnchor: [17, 17],
      });
      L.marker([live.lat, live.lng], { icon, zIndexOffset: 2000 }).bindTooltip(`${escape(live.operadorName ?? 'Chofer')} · ${clock(live.at)}`, { direction: 'top', offset: [0, -16] }).addTo(g);
    }
    // Fit only when the ruta or the day changes, not on every position.
    const fitKey = `${vista.ruta?.id}|${vista.date}|${points.length}`;
    const all = live ? [...points, [live.lat, live.lng] as L.LatLngTuple] : points;
    if (fitKey !== lastFit.current && all.length > 0) {
      lastFit.current = fitKey;
      m.fitBounds(L.latLngBounds(all).pad(0.15), { maxZoom: 15 });
    }
  }, [vista, live]);

  return <div ref={container} className="routes-map__canvas vista-map__canvas" />;
}

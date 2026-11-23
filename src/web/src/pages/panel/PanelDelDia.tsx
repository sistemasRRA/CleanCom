import { lazy, Suspense, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { ApiError, api, unwrap, type Schemas } from '../../api/client';
import { keys } from '../../api/queries';
import { useOperacion, type LiveLocation, type LiveStatus } from '../../api/realtime';
import { useSession } from '../../session/Session';
import { PageHeader, QueryState } from '../../components/Common';
import { Loader } from '../../components/Loader';
import { useConfirm, useNotify } from '../../components/Toasts';

const RoutesMap = lazy(() => import('../../components/RoutesMap').then((m) => ({ default: m.RoutesMap })));

type Panel = Schemas['PanelDto'];
type Card = Schemas['PanelRutaDto'];
type Progress = Schemas['RutaProgress'];

const panelKey = ['panel'] as const;
const liveKey = ['ubicaciones'] as const;

const PROGRESS: Record<Progress, { label: string; tone: string }> = {
  SinSalir: { label: 'Sin salir', tone: '' },
  EnRuta: { label: 'En ruta', tone: 'brand' },
  DeRegreso: { label: 'De regreso', tone: 'success' },
};
const KIND: Partial<Record<Schemas['ParadaKind'], string>> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Entrega y recolección',
};
const STATE: Record<Schemas['ParadaState'], string> = {
  Pendiente: 'Pendiente',
  EnCamino: 'En camino',
  EnSitio: 'En sitio',
  Realizada: 'Realizada',
  NoRealizada: 'No realizada',
};

const money = (n: number) => n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 0 });
const hm = (t?: string | null) => (t ? t.slice(0, 5) : '');
const clock = (iso?: string | null) =>
  iso ? new Date(iso).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit', timeZone: 'America/Mexico_City' }) : '';
const today = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(new Date());
function addDays(iso: string, days: number) {
  const d = new Date(`${iso}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

/**
 * Panel del día (docs/pantallas/oficina/panel-del-dia.md): indicators, pending tray, a card per ruta, the live map,
 * the next 7 days' load and the latest visits. Everything comes computed from GET /panel; the hub "operacion" moves
 * the choferes on the map and reloads the panel when a parada changes.
 */
export function PanelDelDia() {
  const [date, setDate] = useState(today);
  const queryClient = useQueryClient();
  const panel = useQuery({
    queryKey: [...panelKey, date],
    queryFn: () => unwrap(api.GET('/panel', { params: { query: { fecha: date } } })),
    placeholderData: (previous) => previous,
    // Delay and "sin GPS" depend on the clock: refresh every minute even without events.
    refetchInterval: 60_000,
  });
  const plans = useQuery({
    queryKey: [...keys.rutasDia, date],
    queryFn: () => unwrap(api.GET('/rutas/dia', { params: { query: { fecha: date } } })),
    placeholderData: (previous) => previous,
  });
  const live = useLiveLocations(date);

  // A parada that changed: reload the panel and the map (several events together count once).
  const reloadTimer = useRef<number | undefined>(undefined);
  const status = useOperacion({
    onLocation: live.receive,
    onParada: (p) => {
      if (p.date !== date) return;
      window.clearTimeout(reloadTimer.current);
      reloadTimer.current = window.setTimeout(() => {
        void queryClient.invalidateQueries({ queryKey: panelKey });
        void queryClient.invalidateQueries({ queryKey: keys.rutasDia });
      }, 800);
    },
  });
  useEffect(() => () => window.clearTimeout(reloadTimer.current), []);

  const p = panel.data;
  return (
    <>
      <PageHeader
        title="Panel del día"
        description={
          p ? (
            <>
              <span className="rutas-day">{p.label}</span> · salida {hm(p.departure)} · regreso {hm(p.maxReturn)}
            </>
          ) : undefined
        }
      >
        <LiveBadge status={status} />
        <div className="actions">
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
      </PageHeader>

      <QueryState loading={panel.isLoading} error={panel.error}>
        {p && (
          <div className="stack">
            {p.nonWorkingReason && <div className="alert info">Día inhábil: {p.nonWorkingReason}.</div>}
            <Indicators panel={p} />
            <PendingTray panel={p} />
            <section className="panel-rutas">
              {p.rutas.length === 0 ? (
                <div className="empty">No hay rutas activas.</div>
              ) : (
                p.rutas.map((r) => <RutaCard key={r.rutaId} card={r} panel={p} date={date} />)
              )}
            </section>
            <section className="card">
              <div className="card__header">
                <h2>Mapa del día</h2>
                <span className="muted">Paradas en el orden de cada ruta; el chofer se mueve en vivo.</span>
              </div>
              <div className="card__body">
                <Suspense fallback={<Loader size="small" />}>
                  {plans.data ? (
                    <RoutesMap base={{ lat: plans.data.baseLat, lng: plans.data.baseLng }} rutas={plans.data.rutas} live={live.locations} />
                  ) : (
                    <Loader size="small" />
                  )}
                </Suspense>
              </div>
            </section>
            <div className="panel-bottom">
              <WeeklyLoad panel={p} />
              <LatestVisits panel={p} />
            </div>
          </div>
        )}
      </QueryState>
    </>
  );
}

/** Last position per ruta: loaded once, then moved by the hub. */
function useLiveLocations(date: string) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: [...liveKey, date],
    queryFn: () => unwrap(api.GET('/ubicaciones', { params: { query: { fecha: date } } })),
  });
  const receive = useCallback(
    (l: LiveLocation) => {
      if (l.date !== date) return;
      queryClient.setQueryData<LiveLocation[]>([...liveKey, date], (current = []) => [...current.filter((x) => x.rutaDiaId !== l.rutaDiaId), l]);
    },
    [date, queryClient],
  );
  return { locations: query.data ?? [], receive };
}

function LiveBadge({ status }: { status: LiveStatus }) {
  const text: Record<LiveStatus, string> = {
    connecting: 'Conectando…',
    live: 'En vivo',
    reconnecting: 'Reconectando…',
    offline: 'Sin conexión en vivo',
  };
  return (
    <span className={`badge ${status === 'live' ? 'success' : status === 'offline' ? 'danger' : ''}`} title="Actualización en tiempo real">
      {text[status]}
    </span>
  );
}

function Indicators({ panel }: { panel: Panel }) {
  const i = panel.indicators;
  return (
    <div className="planta-stats panel-stats">
      <Stat label="Recolecciones" value={`${i.recolecciones}/${i.recoleccionesPlanned}`} tone="brand" />
      <Stat label="Entregas" value={`${i.entregas}/${i.entregasPlanned}`} tone="brand" />
      <Stat label="Cobrado de referencia" value={money(i.collected)} tone="success" />
      <Stat
        label="Llegadas a tiempo"
        value={i.onTimePercent == null ? '—' : `${i.onTimePercent}%`}
        tone={i.onTimePercent != null && i.onTimePercent < 80 ? 'warning' : 'success'}
      />
      <Stat label="Pedidos en planta" value={String(i.inPlanta)} tone="brand" />
    </div>
  );
}

function Stat({ label, value, tone }: { label: string; value: string; tone: 'brand' | 'success' | 'warning' | 'danger' }) {
  return (
    <div className={`planta-stat ${tone}`}>
      <span className="planta-stat__label">{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

type TrayList = 'pastOpen' | 'ready' | 'planta';

/** One counter per alert type; each opens exactly the list it counts. */
function PendingTray({ panel }: { panel: Panel }) {
  const [open, setOpen] = useState<TrayList | null>(null);
  const { pastOpen, readyWithoutEntrega, longInPlanta } = panel.pending;
  const counters: { key: TrayList; count: number; label: string }[] = [
    { key: 'pastOpen', count: pastOpen.length, label: `parada${pastOpen.length === 1 ? '' : 's'} de días pasados sin cerrar` },
    { key: 'ready', count: readyWithoutEntrega.length, label: `pedido${readyWithoutEntrega.length === 1 ? '' : 's'} listo${readyWithoutEntrega.length === 1 ? '' : 's'} sin fecha de entrega` },
    { key: 'planta', count: longInPlanta.length, label: `pedido${longInPlanta.length === 1 ? '' : 's'} con muchos días en planta` },
  ];
  return (
    <section className="panel-tray" aria-label="Pendientes">
      {counters.map((c) => (
        <button key={c.key} type="button" className={`panel-tray__item${c.count > 0 ? ' warn' : ''}`} disabled={c.count === 0} onClick={() => setOpen(open === c.key ? null : c.key)} aria-expanded={open === c.key}>
          <strong>{c.count}</strong> {c.label}
        </button>
      ))}
      <Link to="/unidades" className={`panel-tray__item${panel.fleetWithPending > 0 ? ' warn' : ''}`}>
        <strong>{panel.fleetWithPending}</strong> unidad{panel.fleetWithPending === 1 ? '' : 'es'} con pendientes
      </Link>

      {open === 'pastOpen' && (
        <TrayPanel title="Paradas de días pasados sin cerrar" action={<Link to="/rutas">Reprogramar en Rutas</Link>}>
          {pastOpen.map((x) => (
            <li key={x.paradaId}>
              <span>
                <strong>{x.name ?? 'Parada'}</strong> · {KIND[x.kind] ?? x.kind} · Ruta {x.rutaCode}
              </span>
              <Link to={`/rutas?fecha=${x.date}`}>{x.date}</Link>
            </li>
          ))}
        </TrayPanel>
      )}
      {open === 'ready' && (
        <TrayPanel title="Pedidos 100% listos sin fecha de entrega" action={<Link to="/pedidos">Confirmar entregas en Pedidos</Link>}>
          {readyWithoutEntrega.map((x) => (
            <li key={x.pedidoId}>
              <span>
                <strong>Pedido {x.pedidoId}</strong> · {x.invitadoName}
              </span>
              <span className="muted">listo hace {x.days} día{x.days === 1 ? '' : 's'}</span>
            </li>
          ))}
        </TrayPanel>
      )}
      {open === 'planta' && (
        <TrayPanel title="Pedidos con muchos días en planta" action={<Link to="/tickets-en-planta">Ver planta</Link>}>
          {longInPlanta.map((x) => (
            <li key={x.pedidoId}>
              <span>
                <strong>Pedido {x.pedidoId}</strong> · {x.invitadoName}
              </span>
              <span className="muted">{x.days} días en planta</span>
            </li>
          ))}
        </TrayPanel>
      )}
    </section>
  );
}

function TrayPanel({ title, action, children }: { title: string; action: ReactNode; children: ReactNode }) {
  return (
    <div className="panel-tray__list card">
      <div className="card__header">
        <h3>{title}</h3>
        {action}
      </div>
      <ul className="plain-list">{children}</ul>
    </div>
  );
}

function RutaCard({ card: r, panel, date }: { card: Card; panel: Panel; date: string }) {
  const { can } = useSession();
  const canEdit = can('panel-del-dia', 'edit') || can('rutas', 'edit');
  const progress = PROGRESS[r.progress];
  const goalPct = Math.min(100, (r.recolecciones / Math.max(r.goal, 1)) * 100);
  return (
    <article className={`card panel-ruta${r.alerts.length > 0 ? ' alert-left' : ''}`}>
      <div className="card__header">
        <div className="ruta-title">
          <span className="ruta-color big" style={{ background: r.color }} />
          <div>
            <h2>Ruta {r.code}</h2>
            <span className="muted">{r.zonas.length > 0 ? r.zonas.join(', ') : r.name}</span>
          </div>
        </div>
        <span className={`badge ${progress.tone}`}>{progress.label}</span>
      </div>
      <div className="card__body stack">
        <div>
          <div className="panel-goal">
            <span>
              Recolecciones <strong>{r.recolecciones}</strong> de {r.recoleccionesPlanned} · meta {r.goal}
            </span>
            <span>
              Entregas <strong>{r.entregas}</strong> de {r.entregasPlanned}
            </span>
          </div>
          <span className="occupancy__bar" aria-hidden>
            <span style={{ width: `${goalPct}%`, background: r.color }} />
          </span>
        </div>
        <div className="ruta-stats">
          <span>
            {r.closed}/{r.paradas} paradas
          </span>
          <span>{r.km.toFixed(1)} km</span>
          <span className={r.returnAt > r.maxReturn ? 'warn' : ''}>regreso {hm(r.returnAt)}</span>
          <span>cobrado {money(r.collected)}</span>
          <span>{r.lastLocationAt ? `GPS ${clock(r.lastLocationAt)}` : 'sin GPS'}</span>
        </div>
        <Assignments card={r} panel={panel} date={date} canEdit={canEdit} />
        {r.alerts.length > 0 && (
          <ul className="panel-alerts">
            {r.alerts.map((a) => (
              <li key={a}>{a}</li>
            ))}
          </ul>
        )}
        <Link to={`/rutas?fecha=${date}`} className="panel-link">
          Ver ruta
        </Link>
      </div>
    </article>
  );
}

/** Chofer and unidad of the day, chosen from the card. A unidad with something pending asks to confirm. */
function Assignments({ card: r, panel, date, canEdit }: { card: Card; panel: Panel; date: string; canEdit: boolean }) {
  const queryClient = useQueryClient();
  const notify = useNotify();
  const ask = useConfirm();
  const [busy, setBusy] = useState(false);
  const past = date < today();

  /** Runs a change and refreshes; an action that returns `false` was cancelled by the user (no message). */
  async function run(action: () => Promise<unknown>, message: string) {
    setBusy(true);
    try {
      if ((await action()) === false) return;
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: panelKey }),
        queryClient.invalidateQueries({ queryKey: keys.rutasDia }),
      ]);
      notify(message);
    } catch (e) {
      notify((e as Error).message, true);
    } finally {
      setBusy(false);
    }
  }

  const setChofer = (operadorId: number | null) =>
    run(() => unwrap(api.PUT('/rutas/{rutaId}/dias/{fecha}/chofer', { params: { path: { rutaId: r.rutaId, fecha: date } }, body: { operadorId } })), 'Chofer asignado.');

  const setUnidad = (unidadId: number | null) =>
    run(async () => {
      const put = (confirm: boolean) =>
        unwrap(api.PUT('/rutas/{rutaId}/dias/{fecha}/unidad', { params: { path: { rutaId: r.rutaId, fecha: date } }, body: { unidadId, confirm } }));
      try {
        await put(false);
      } catch (e) {
        // 409 with a question: the unidad has something pending; the API says what.
        if (!(e instanceof ApiError && e.status === 409 && e.message.includes('¿'))) throw e;
        if (!(await ask(e.message, { confirmText: 'Asignar de todos modos' }))) return false;
        await put(true);
      }
    }, 'Unidad asignada.');

  const operadores = panel.operadores;
  const unidades = panel.unidades;
  const disabled = !canEdit || busy || past;
  return (
    <div className="form-grid panel-assign">
      <label className="field">
        <span>Chofer</span>
        <select value={r.operadorId ?? ''} disabled={disabled} onChange={(e) => void setChofer(e.target.value ? Number(e.target.value) : null)}>
          <option value="">{r.operadorIsUsual ? 'Chofer habitual' : 'Sin chofer'}</option>
          {operadores.map((o) => (
            <option key={o.id} value={o.id}>
              {o.name}
              {r.operadorIsUsual && r.operadorId === o.id ? ' (habitual)' : ''}
            </option>
          ))}
        </select>
      </label>
      <label className="field">
        <span>Unidad</span>
        <select value={r.unidadId ?? ''} disabled={disabled} onChange={(e) => void setUnidad(e.target.value ? Number(e.target.value) : null)}>
          <option value="">Sin unidad</option>
          {unidades.map((u) => (
            <option key={u.id} value={u.id} title={u.note ?? undefined}>
              {u.name}
              {u.note ? ' ⚠' : ''}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}

/** Next 7 days against each ruta's cupo; a full day stands out and opens Rutas on that day. */
function WeeklyLoad({ panel }: { panel: Panel }) {
  const navigate = useNavigate();
  const rutas = panel.load.rutas;
  const days = useMemo(() => panel.load.days, [panel.load.days]);
  return (
    <section className="card">
      <div className="card__header">
        <h2>Carga de los próximos 7 días</h2>
        <span className="muted">Paradas contra el cupo de cada ruta, con las fijas.</span>
      </div>
      <div className="card__body panel-load">
        {days.map((d) => {
          const over = d.rutas.some((x) => x.occupied > x.cupo);
          return (
            <button key={d.date} type="button" className={`panel-load__day${over ? ' over' : ''}`} onClick={() => navigate(`/rutas?fecha=${d.date}`)} title="Planear este día en Rutas">
              <span className="panel-load__label">{d.label}</span>
              {d.nonWorking ? (
                <span className="muted">Inhábil</span>
              ) : (
                d.rutas.map((x) => {
                  const ruta = rutas.find((r) => r.id === x.rutaId);
                  const full = x.occupied >= x.cupo;
                  return (
                    <span key={x.rutaId} className={`occupancy${full ? ' full' : ''}`}>
                      <span className="occupancy__label">
                        <span className="ruta-color" style={{ background: ruta?.color }} />
                        {ruta?.code} {x.occupied}/{x.cupo}
                      </span>
                      <span className="occupancy__bar">
                        <span style={{ width: `${Math.min(100, (x.occupied / Math.max(x.cupo, 1)) * 100)}%`, background: full ? undefined : ruta?.color }} />
                      </span>
                    </span>
                  );
                })
              )}
            </button>
          );
        })}
      </div>
    </section>
  );
}

function LatestVisits({ panel }: { panel: Panel }) {
  return (
    <section className="card">
      <div className="card__header">
        <h2>Últimas visitas</h2>
        {panel.latestVisits.length > 0 && <Link to="/pedidos">Ver evidencias</Link>}
      </div>
      <div className="card__body">
        {panel.latestVisits.length === 0 ? (
          <div className="empty">Todavía no hay visitas cerradas este día.</div>
        ) : (
          <ul className="plain-list panel-visits">
            {panel.latestVisits.map((v) => (
              <li key={v.paradaId}>
                <span>
                  <strong>{v.name ?? 'Parada'}</strong> · Ruta {v.rutaCode} · {KIND[v.kind] ?? v.kind}
                  <span className={`badge ${v.state === 'Realizada' ? 'success' : 'danger'}`}>{STATE[v.state]}</span>
                </span>
                <span className="muted">
                  {clock(v.closedAt)} · {v.photos} foto{v.photos === 1 ? '' : 's'}
                  {v.signed ? ' · firma' : ''}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}

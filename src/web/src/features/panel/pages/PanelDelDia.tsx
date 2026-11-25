import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { addDays, hm, todayInMexicoCity } from '../../../shared/utils/dates';
import { panelKey, useLiveLocations } from '../services/queries';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { useOperacion } from '../../../shared/api/realtime';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { LiveBadge } from '../components/LiveBadge';
import { Indicators } from '../components/Indicators';
import { PendingTray } from '../components/PendingTray';
import { RutaCard } from '../components/RutaCard';
import { Loader } from '../../../shared/components/Loader';
import { WeeklyLoad } from '../components/WeeklyLoad';
import { LatestVisits } from '../components/LatestVisits';
import { rutasApi } from '../../rutas';
import { panelApi } from '../services/panelApi';

const RoutesMap = lazy(() => import('../../../shared/components/RoutesMap').then((m) => ({ default: m.RoutesMap })));

/**
 * Panel del día (docs/pantallas/oficina/panel-del-dia.md): indicators, pending tray, a card per ruta, the live map,
 * the next 7 days' load and the latest visits. Everything comes computed from GET /panel; the hub "operacion" moves
 * the choferes on the map and reloads the panel when a parada changes.
 */
export function PanelDelDia() {
  const [date, setDate] = useState(todayInMexicoCity);
  const queryClient = useQueryClient();
  const panel = useQuery({
    queryKey: [...panelKey, date],
    queryFn: () => panelApi.get({ fecha: date }),
    placeholderData: (previous) => previous,
    // Delay and "sin GPS" depend on the clock: refresh every minute even without events.
    refetchInterval: 60_000,
  });
  const plans = useQuery({
    queryKey: [...keys.rutasDia, date],
    queryFn: () => rutasApi.day({ fecha: date }),
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
          {date !== todayInMexicoCity() && (
            <button type="button" className="small" onClick={() => setDate(todayInMexicoCity())}>
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

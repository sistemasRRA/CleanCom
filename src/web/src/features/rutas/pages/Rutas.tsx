import { lazy, Suspense, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { addDays, todayInMexicoCity } from '../../../shared/utils/dates';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { RutaCard } from '../components/RutaCard';
import { Loader } from '../../../shared/components/Loader';
import { rutasApi } from '../services/rutasApi';

// Leaflet loads only with the map.
const RoutesMap = lazy(() => import('../../../shared/components/RoutesMap').then((m) => ({ default: m.RoutesMap })));

/**
 * Rutas (docs/pantallas/oficina/rutas.md): the plan of each ruta for a day with chofer, order, estimated times,
 * lunch, return to Leones and alerts. Everything is calculated by the API.
 */
export function Rutas() {
  // ?fecha=yyyy-MM-dd opens that day (from the Panel del día).
  const [params] = useSearchParams();
  const [date, setDate] = useState(() => {
    const fecha = params.get('fecha') ?? '';
    return /^\d{4}-\d{2}-\d{2}$/.test(fecha) ? fecha : todayInMexicoCity();
  });
  const plan = useQuery({
    queryKey: [...keys.rutasDia, date],
    queryFn: () => rutasApi.day({ fecha: date }),
    placeholderData: (previous) => previous,
  });
  const d = plan.data;

  return (
    <div className="stack">
      <PageHeader title="Rutas" description="Planeación del día por ruta: chofer, paradas en orden, horas estimadas y regreso a Leones.">
        <button type="button" className="small" onClick={() => setDate(addDays(date, -1))}>
          ◀
        </button>
        <input type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} style={{ width: 'auto' }} />
        <button type="button" className="small" onClick={() => setDate(addDays(date, 1))}>
          ▶
        </button>
        <button type="button" className="small" onClick={() => setDate(todayInMexicoCity())}>
          Hoy
        </button>
      </PageHeader>
      <QueryState loading={plan.isPending} error={plan.error}>
        {d && (
          <>
            <p className="muted rutas-note">
              <strong className="rutas-day">{d.label}</strong>
              {d.nonWorkingReason && <span className="badge warning">Día inhábil: {d.nonWorkingReason}</span>} {d.estimate}
            </p>
            {/* Side by side on a wide screen (F, K…), one under the other on a narrow one; the map goes below. */}
            <div className="rutas-grid">
              {d.rutas.map((r) => (
                <RutaCard key={r.rutaId} plan={r} date={d.date} />
              ))}
            </div>
            {d.rutas.length > 0 && (
              <section className="card">
                <div className="card__header">
                  <h2>Mapa</h2>
                  <span className="muted">
                    Los números siguen el orden de visita de cada ruta; las líneas van en línea recta hasta conectar Google.
                  </span>
                </div>
                <div className="card__body">
                  <Suspense
                    fallback={
                      <div className="routes-map__canvas">
                        <Loader label="Cargando mapa…" />
                      </div>
                    }
                  >
                    <RoutesMap base={{ lat: d.baseLat, lng: d.baseLng }} rutas={d.rutas} />
                  </Suspense>
                </div>
              </section>
            )}
          </>
        )}
      </QueryState>
    </div>
  );
}

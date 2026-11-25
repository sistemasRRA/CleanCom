import { useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import { useRutas } from '../../operacion';
import { addDays, clock, todayInMexicoCity } from '../../../shared/utils/dates';
import '../../../shared/api/client';
import { type LiveLocation, useOperacion } from '../../../shared/api/realtime';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { ParadaItem } from '../components/ParadaItem';
import { ChoferMap } from '../components/ChoferMap';
import { choferApi } from '../services/choferApi';

const previewKey = ['chofer', 'vista'] as const;

/**
 * Vista del chofer (docs/pantallas/oficina/vista-del-chofer.md): a read-only preview of what the chofer of a ruta sees
 * in the app (GET /chofer/vista, the same calculation), with each parada's state and the chofer's position, live.
 */
export function VistaDelChofer() {
  const rutas = useRutas();
  const activas = (rutas.data ?? []).filter((r) => r.active);
  const [rutaId, setRutaId] = useState<number | null>(null);
  const [date, setDate] = useState(todayInMexicoCity);
  const selected = rutaId ?? activas[0]?.id ?? null;
  const queryClient = useQueryClient();

  const vista = useQuery({
    queryKey: [...previewKey, selected, date],
    enabled: selected != null,
    queryFn: () => choferApi.vista({ ruta: selected!, fecha: date }),
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
        <span className={`badge ${status === 'live' ? 'success' : status === 'offline' ? 'danger' : ''}`}>
          {status === 'live' ? 'En vivo' : status === 'offline' ? 'Sin conexión en vivo' : 'Conectando…'}
        </span>
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
        {date !== todayInMexicoCity() && (
          <button type="button" className="small" onClick={() => setDate(todayInMexicoCity())}>
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
                    {live
                      ? `${live.live ? 'Ubicación' : 'Última ubicación guardada'} de las ${clock(live.at)}`
                      : 'Sin ubicación del chofer este día.'}
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

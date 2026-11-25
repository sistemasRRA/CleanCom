import { useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { useRutas, useZonas } from '../services/queries';
import { useConfirm } from '../../../shared/components/Toasts';
import { mapZonas, type Zona } from '../types/rutasZonas';
import { useAction } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { PolygonMap } from '../components/PolygonMap';
import { WeekdayBadges } from '../components/WeekdayBadges';
import { ZonaForm } from '../components/ZonaForm';
import { VisitDaysForm } from '../components/VisitDaysForm';
import { operacionApi } from '../services/operacionApi';

// ---------------- Zonas ----------------

export function Zonas() {
  const canEdit = useSession().can('operacion', 'edit');
  const zonas = useZonas();
  const rutas = useRutas();
  const ask = useConfirm();
  const [editing, setEditing] = useState<Zona | 'new' | null>(null);
  const [days, setDays] = useState<Zona | null>(null);
  const drawn = mapZonas(zonas.data ?? [], rutas.data ?? []);
  const deactivate = useAction((id: number) => operacionApi.removeZona(id), [keys.zonas], 'Zona desactivada.');

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Zonas</h2>
          <p className="muted">Las zonas EXT siguen los días de visita de su núcleo.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva zona
          </button>
        )}
      </div>
      <QueryState loading={zonas.isPending} error={zonas.error}>
        {drawn.length > 0 && (
          <div className="card__body">
            <PolygonMap others={drawn} className="polygon-map overview" />
          </div>
        )}
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Zona</th>
                <th>Ruta</th>
                <th>Núcleo</th>
                <th>Días de visita</th>
                <th>Polígono</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {zonas.data?.map((z) => (
                <tr key={z.id} className={z.active ? '' : 'inactive'}>
                  <td>
                    <strong>{z.code}</strong>
                  </td>
                  <td>{z.rutaCode}</td>
                  <td>{z.zonaNucleoCode ?? <span className="muted">es núcleo</span>}</td>
                  <td>
                    <WeekdayBadges days={z.effectiveVisitDays} />
                    {z.zonaNucleoId && (
                      <div className="muted" style={{ fontSize: 12 }}>
                        heredados de {z.zonaNucleoCode}
                      </div>
                    )}
                  </td>
                  <td className="muted">{z.polygon?.length ? `${z.polygon.length} vértices` : 'sin dibujar'}</td>
                  <td>
                    <ActiveBadge active={z.active} />
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        {!z.zonaNucleoId && (
                          <button type="button" className="small" onClick={() => setDays(z)}>
                            Días
                          </button>
                        )}
                        <button type="button" className="small" onClick={() => setEditing(z)}>
                          Editar
                        </button>
                        {z.active && (
                          <button
                            type="button"
                            className="small danger"
                            disabled={deactivate.isPending}
                            onClick={async () =>
                              (await ask(`¿Desactivar la zona ${z.code}?`, { confirmText: 'Desactivar', danger: true })) && deactivate.mutate(z.id)
                            }
                          >
                            Desactivar
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {zonas.data?.length === 0 && <div className="empty">Todavía no hay zonas.</div>}
        </div>
      </QueryState>
      {editing && <ZonaForm zona={editing === 'new' ? null : editing} zonas={zonas.data ?? []} onClose={() => setEditing(null)} />}
      {days && <VisitDaysForm zona={days} onClose={() => setDays(null)} />}
    </section>
  );
}

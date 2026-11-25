import { Fragment, useState } from 'react';
import { hm, type Plan } from '../types/rutas';
import { useSession } from '../../../shared/session/Session';
import { useConfirm } from '../../../shared/components/Toasts';
import { useOperadores } from '../../usuarios';
import { keys } from '../../../shared/api/queryKeys';
import { useAction } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { ParadaRow } from './ParadaRow';
import { AddDialog } from './AddParadaDialog';
import { rutasApi } from '../services/rutasApi';

export function RutaCard({ plan: r, date }: { plan: Plan; date: string }) {
  const canEdit = useSession().can('rutas', 'edit');
  const ask = useConfirm();
  const operadores = useOperadores();
  const refetch = [keys.rutasDia, keys.agendaBoard];
  const assign = useAction((operadorId: number | null) => rutasApi.assignChofer(r.rutaId, date, { operadorId }), refetch, 'Chofer asignado.');
  const optimize = useAction(() => rutasApi.optimize(r.rutaDiaId!), refetch, `Ruta ${r.code} optimizada.`);
  const patch = useAction(
    ({ id, position, fixed }: { id: number; position: number | null; fixed: boolean | null }) => rutasApi.updateParada(id, { position, fixed }),
    refetch,
    'Orden actualizado.',
  );
  const remove = useAction((id: number) => rutasApi.removeParada(id), refetch, 'Quitada de la ruta.');
  const [adding, setAdding] = useState(false);
  const busy = assign.isPending || optimize.isPending || patch.isPending || remove.isPending;
  const late = r.returnAt > r.maxReturn;

  return (
    <section className="card">
      <div className="card__header">
        <div className="ruta-title">
          <span className="ruta-color big" style={{ background: r.color }} />
          <div>
            <h2>{r.name}</h2>
            <div className="ruta-stats muted">
              <span className={r.occupied > r.cupo ? 'warn' : ''}>
                {r.occupied} de {r.cupo} paradas
              </span>
              <span>Sale {hm(r.departure)}</span>
              {r.lunchStart && <span>Comida {hm(r.lunchStart)}</span>}
              <span className={late ? 'warn' : ''}>
                Regresa {hm(r.returnAt)} {late && `(máx. ${hm(r.maxReturn)})`}
              </span>
              <span>{r.km} km</span>
              <span className={r.garments > r.capacity ? 'warn' : ''}>
                {r.garments} de {r.capacity} prendas
              </span>
            </div>
          </div>
        </div>
        <div className="actions">
          <label className="inline-field">
            <span className="muted">Chofer</span>
            <select
              value={r.operadorIsUsual ? '' : (r.operadorId ?? '')}
              disabled={!canEdit || busy}
              onChange={(e) => assign.mutate(e.target.value ? Number(e.target.value) : null)}
              style={{ width: 'auto' }}
            >
              <option value="">{r.operadorIsUsual ? `${r.operadorName} (habitual)` : 'Habitual de la ruta'}</option>
              {operadores.data
                ?.filter((o) => o.active)
                .map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.name}
                  </option>
                ))}
            </select>
          </label>
          {canEdit && (
            <button type="button" disabled={busy} onClick={() => setAdding(true)}>
              Agregar
            </button>
          )}
          {canEdit && r.rutaDiaId && (
            <button type="button" className="primary" disabled={busy || r.paradas.length === 0} onClick={() => optimize.mutate(undefined)}>
              {optimize.isPending ? 'Optimizando…' : 'Optimizar'}
            </button>
          )}
        </div>
      </div>

      {r.alerts.length > 0 && (
        <div className="alert warning ruta-alerts">
          <ul>
            {r.alerts.map((a) => (
              <li key={a}>{a}</li>
            ))}
          </ul>
        </div>
      )}

      {r.paradas.length === 0 ? (
        <div className="empty">Sin paradas este día.</div>
      ) : (
        <div className="table-wrap">
          <table className="ruta-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Hora</th>
                <th>Parada</th>
                <th>Qué se hace</th>
                <th>Ventana</th>
                <th>Km</th>
                {canEdit && <th></th>}
              </tr>
            </thead>
            <tbody>
              <tr className="ruta-base">
                <td></td>
                <td className="num">{hm(r.departure)}</td>
                <td colSpan={canEdit ? 5 : 4}>Salida de Leones (carga de la unidad)</td>
              </tr>
              {r.paradas.map((p, i) => (
                <Fragment key={p.id}>
                  <ParadaRow
                    p={p}
                    first={i === 0}
                    last={i === r.paradas.length - 1}
                    canEdit={canEdit}
                    busy={busy}
                    onMove={(by) => patch.mutate({ id: p.id, position: p.order + by, fixed: null })}
                    onPin={() => patch.mutate({ id: p.id, position: null, fixed: !p.positionFixed })}
                    onRemove={async () =>
                      (await ask(
                        p.viaje
                          ? `¿Quitar todo el viaje "${p.viaje.split(' · ')[0]}" de la ruta este día?`
                          : `¿Quitar "${p.name}" de la ruta este día?`,
                        { confirmText: 'Quitar', danger: true },
                      )) && remove.mutate(p.id)
                    }
                  />
                  {p.lunchAfter && (
                    <tr className="ruta-lunch">
                      <td></td>
                      <td className="num">{hm(p.leave)}</td>
                      <td colSpan={canEdit ? 5 : 4}>Comida del chofer</td>
                    </tr>
                  )}
                </Fragment>
              ))}
              <tr className={`ruta-base${late ? ' late' : ''}`}>
                <td></td>
                <td className="num">{hm(r.returnAt)}</td>
                <td colSpan={canEdit ? 5 : 4}>Entrega de ruta en Leones (descarga)</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
      {adding && <AddDialog ruta={r} date={date} onClose={() => setAdding(false)} />}
    </section>
  );
}

import { CATEGORY, clock, hm, KIND, type Parada } from '../types/rutas';

export function ParadaRow({
  p,
  first,
  last,
  canEdit,
  busy,
  onMove,
  onPin,
  onRemove,
}: {
  p: Parada;
  first: boolean;
  last: boolean;
  canEdit: boolean;
  busy: boolean;
  onMove: (by: number) => void;
  onPin: () => void;
  onRemove: () => void;
}) {
  return (
    <tr className={p.outOfWindow ? 'late' : ''}>
      <td className="num">
        <span className="ruta-order">{p.order}</span>
      </td>
      <td className="num">
        <strong>{hm(p.arrival)}</strong>
        {p.waitMinutes > 0 && <div className="muted">espera {p.waitMinutes} min</div>}
      </td>
      <td>
        <strong>{p.name}</strong>
        {p.priority && <span className="badge danger ruta-flag">Prioridad</span>}
        {p.placement === 'Primera' && <span className="badge brand ruta-flag">1ª</span>}
        {p.placement === 'Ultima' && <span className="badge brand ruta-flag">Últ.</span>}
        {p.viaje && <div className="ruta-viaje">{p.viaje}</div>}
        {p.activity && <div>{p.activity}</div>}
        <div className="muted">
          {p.address ?? 'Sin dirección'}
          {p.zonaCode && ` · ${p.zonaCode}`}
        </div>
        {p.notes && <div className="muted">Nota: {p.notes}</div>}
        {p.noLocation && <div className="badge warning ruta-flag">Sin coordenadas</div>}
        {p.state === 'EnCamino' && <div className="badge brand ruta-flag">En camino desde las {clock(p.enCaminoAt)}</div>}
        {p.state === 'EnSitio' && (
          <div className={`badge ${p.arrivedOutsideRadius ? 'warning' : 'success'} ruta-flag`}>
            Llegó a las {clock(p.arrivedAt)}
            {p.arrivedOutsideRadius && ' · fuera de radio'}
          </div>
        )}
      </td>
      <td>
        {p.category && p.category !== 'ClienteFinal' && <div className="muted">{CATEGORY[p.category]}</div>}
        {KIND[p.kind]}
        {p.garmentsApprox != null && <div className="muted">{p.garmentsApprox} prendas</div>}
      </td>
      <td className="num">
        {hm(p.windowStart)} a {hm(p.windowEnd)}
        {p.outOfWindow && <div className="badge danger ruta-flag">Fuera de ventana</div>}
      </td>
      <td className="num muted">{p.kmFromPrevious}</td>
      {canEdit && (
        <td className="end">
          <div className="actions tight end">
            <button type="button" className="small" disabled={busy || first} onClick={() => onMove(-1)} title="Subir">
              ▲
            </button>
            <button type="button" className="small" disabled={busy || last} onClick={() => onMove(1)} title="Bajar">
              ▼
            </button>
            <button
              type="button"
              className={`small${p.positionFixed ? ' selected' : ''}`}
              disabled={busy}
              onClick={onPin}
              title={p.positionFixed ? 'Fija: optimizar no la mueve. Clic para soltarla.' : 'Fijar en esta posición'}
            >
              {p.positionFixed ? '📌 Fija' : 'Fijar'}
            </button>
            {p.pedidoId == null && p.state === 'Pendiente' && (
              <button type="button" className="small danger" disabled={busy} onClick={onRemove} title="Quitar de la ruta este día">
                Quitar
              </button>
            )}
          </div>
        </td>
      )}
    </tr>
  );
}

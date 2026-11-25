import { KIND, type Parada, STATE } from '../types/chofer';
import { clock, hm } from '../../../shared/utils/dates';

export function ParadaItem({ parada: p, next }: { parada: Parada; next: boolean }) {
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

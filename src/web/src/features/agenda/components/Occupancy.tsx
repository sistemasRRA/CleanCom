import type { Ruta } from '../types/board';

export function Occupancy({ ruta, occupied, cupo }: { ruta: Ruta; occupied: number; cupo: number }) {
  const full = occupied >= cupo;
  return (
    <div className={`occupancy${full ? ' full' : ''}`} title={`Ruta ${ruta.code}: ${occupied} de ${cupo} paradas`}>
      <span className="occupancy__label">
        <span className="ruta-color" style={{ background: ruta.color }} />
        {ruta.code} {occupied}/{cupo}
        {full && <strong> lleno</strong>}
      </span>
      <span className="occupancy__bar">
        <span style={{ width: `${Math.min(100, (occupied / Math.max(cupo, 1)) * 100)}%`, background: full ? undefined : ruta.color }} />
      </span>
    </div>
  );
}

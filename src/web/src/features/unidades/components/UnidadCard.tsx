import { useQuery } from '@tanstack/react-query';
import { type Card, day, km, LEVEL_TONE, money } from '../types/unidades';
import { ActiveBadge } from '../../../shared/components/Page';
import '../../../shared/api/client';
import { LABEL as TYPE_LABEL, UnidadIcon } from './UnidadIcon';
import { unidadesApi } from '../services/unidadesApi';

export function UnidadCard({ unidad: u, onOpen }: { unidad: Card; onOpen: () => void }) {
  return (
    <button type="button" className={`unidad-card${u.pending ? ' pending' : ''}${u.active ? '' : ' inactive'}`} onClick={onOpen}>
      <div className="unidad-card__head">
        <UnidadAvatar unidad={u} />
        <div>
          <header>
            <strong className="unidad-plate">{u.plate}</strong>
            {u.state === 'FueraDeServicio' && <span className="badge danger">Fuera de servicio</span>}
            {!u.active && <ActiveBadge active={false} />}
          </header>
          <span className="muted">
            {u.brand} {u.model} {u.year} · {km(u.km)}
            {u.kmUpdatedAt && ` al ${day(u.kmUpdatedAt)}`}
          </span>
        </div>
      </div>
      {u.alerts.length > 0 && (
        <ul className="unidad-alerts">
          {u.alerts.map((a, i) => (
            <li key={i} className={LEVEL_TONE[a.level]}>
              {a.message}
            </li>
          ))}
        </ul>
      )}
      <dl className="unidad-facts">
        <dt>Último servicio</dt>
        <dd>{u.lastServiceOn ? `${day(u.lastServiceOn)} · ${km(u.lastServiceKm)}` : 'Sin registro'}</dd>
        <dt>Próximo servicio</dt>
        <dd>{u.nextServiceOn || u.nextServiceKm ? `${day(u.nextServiceOn)} o ${km(u.nextServiceKm)}` : '—'}</dd>
        <dt>Seguro</dt>
        <dd>{u.insuranceUntil ? `hasta ${day(u.insuranceUntil)}` : '—'}</dd>
        <dt>Tarjeta</dt>
        <dd>{u.circulationCardUntil ? `hasta ${day(u.circulationCardUntil)}` : '—'}</dd>
        <dt>Verificación</dt>
        <dd>{u.verificationExempt ? 'Exenta' : u.lastVerifiedOn ? day(u.lastVerifiedOn) : '—'}</dd>
        <dt>Gasto del año</dt>
        <dd>
          {money(u.spentThisYear)}
          {u.costPerKm != null && ` · ${money(u.costPerKm)}/km`}
        </dd>
      </dl>
    </button>
  );
}

/** The unidad's photo, or the icon of its type while it has none. */
function UnidadAvatar({ unidad: u }: { unidad: Card }) {
  const photo = useQuery({
    queryKey: ['unidades', u.id, 'foto', u.photoId],
    enabled: u.photoId != null,
    staleTime: Infinity,
    queryFn: async () => URL.createObjectURL((await unidadesApi.documentFile(u.id, u.photoId!)) as Blob),
  });
  return (
    <span className={`unidad-avatar type-${u.type.toLowerCase()}`} title={TYPE_LABEL[u.type]}>
      {photo.data ? <img src={photo.data} alt={`Foto de ${u.plate}`} /> : <UnidadIcon type={u.type} size={56} />}
    </span>
  );
}

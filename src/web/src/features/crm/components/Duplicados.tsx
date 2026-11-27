import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import { type Duplicate } from '../types/crm';
import { useAction } from '../../../shared/hooks/useSave';
import { keys } from '../../../shared/api/queryKeys';
import { formatDate } from '../../../shared/utils/dates';
import { invitadosApi } from '../../invitados';

/**
 * Other invitados who share a phone with this one. Merging moves everything here and gives the duplicate a baja
 * lógica; it cannot be undone, so only SuperUser and Administración see the button (the API checks it again).
 */
export function Duplicados({ invitadoId, name }: { invitadoId: number; name: string }) {
  const role = useSession().user?.role;
  const canMerge = role === 'SuperUser' || role === 'Administracion';
  const duplicados = useQuery({
    queryKey: [...keys.crm, 'duplicados', invitadoId],
    queryFn: () => invitadosApi.duplicados(invitadoId),
  });
  const merge = useAction(
    (d: Duplicate) => invitadosApi.fusionar(invitadoId, { duplicadoId: d.id }),
    [keys.crm],
    'Invitados fusionados.',
  );
  const list = duplicados.data ?? [];
  if (list.length === 0) return null;

  return (
    <div className="alert stack">
      <strong>Posible duplicado: comparte teléfono con {list.length === 1 ? 'otro invitado' : `${list.length} invitados`}.</strong>
      {list.map((d) => (
        <div key={d.id} className="actions" style={{ justifyContent: 'space-between' }}>
          <span>
            <strong>{d.name}</strong>
            <span className="muted">
              {' '}
              · {d.whatsAppPhone}
              {d.otherPhone && ` / ${d.otherPhone}`}
              {d.email && ` · ${d.email}`} · {d.pedidos} pedidos{d.hasApp && ' · con app'} · alta {formatDate(d.createdAt)}
            </span>
          </span>
          {canMerge && (
            <button
              type="button"
              className="small"
              disabled={merge.isPending}
              onClick={() =>
                confirm(
                  `¿Fusionar a ${d.name} en ${name}? Sus pedidos, direcciones, contactos, beneficios y cuenta de app pasan a ${name} y ${d.name} se da de baja. No se puede deshacer.`,
                ) && merge.mutate(d)
              }
            >
              Fusionar aquí
            </button>
          )}
        </div>
      ))}
    </div>
  );
}

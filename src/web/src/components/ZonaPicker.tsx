import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../api/client';
import { keys } from '../api/queries';
import { useSession } from '../session/Session';
import { useSave } from './Common';

type Address = Schemas['AddressDto'];

/**
 * Zona of an invitado's address: the badge and, for whoever edits Agendar, choosing it by hand, changing it or going
 * back to automatic (polygon or Operación › Cobertura). The API decides the zona; this only sends the choice.
 */
export function ZonaPicker({ invitadoId, address: d }: { invitadoId: number; address: Address }) {
  const canEdit = useSession().can('agendar', 'edit');
  const [changing, setChanging] = useState(false);
  const picking = canEdit && (changing || !d.zonaId);
  const zonas = useQuery({ queryKey: [...keys.zonas, 'activas'], queryFn: () => unwrap(api.GET('/zonas')), enabled: picking });
  const assign = useSave(
    (zonaId: number | null) =>
      unwrap(
        api.PUT('/invitados/{id}/direcciones/{addressId}', {
          params: { path: { id: invitadoId, addressId: d.id } },
          body: { ...d, zonaId },
        }),
      ),
    {
      refetch: [['invitados'], ['agenda'], keys.crm],
      message: 'Zona actualizada.',
      onDone: () => setChanging(false),
    },
  );

  return (
    <div className="actions tight">
      {d.zonaCode ? (
        <span className="badge brand">
          Zona {d.zonaCode}
          {d.zonaAssignedManually && ' (a mano)'}
        </span>
      ) : (
        <span className="badge warning">Fuera de cobertura</span>
      )}
      {canEdit && d.zonaId && !changing && (
        <button type="button" className="small" onClick={() => setChanging(true)}>
          Cambiar
        </button>
      )}
      {picking && (
        <select
          value=""
          aria-label="Zona de la dirección"
          onChange={(e) => {
            const v = e.target.value;
            if (v) assign.mutate(v === 'auto' ? null : Number(v));
          }}
          disabled={assign.isPending}
        >
          <option value="">{d.zonaId ? 'Elegir otra zona…' : 'Elegir zona…'}</option>
          {d.zonaAssignedManually && <option value="auto">Automática (por cobertura)</option>}
          {zonas.data
            ?.filter((z) => z.id !== d.zonaId)
            .map((z) => (
              <option key={z.id} value={z.id}>
                {z.code} ({z.rutaCode})
              </option>
            ))}
        </select>
      )}
      {changing && (
        <button type="button" className="small" onClick={() => setChanging(false)}>
          Cancelar
        </button>
      )}
      {assign.error && <div className="alert error">{assign.error.message}</div>}
    </div>
  );
}

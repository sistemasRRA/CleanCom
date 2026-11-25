import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import type { Invitado } from '../types/invitados';
import { useConfirm, useNotify } from '../../../shared/components/Toasts';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { DireccionForm, DireccionOption } from './DireccionForm';
import { InvitadoForm } from './InvitadoForm';
import { invitadosApi } from '../services/invitadosApi';
import { pedidosApi } from '../../pedidos';

/** Deleting is a baja lógica, fixed to SuperUser and Administración in the API. */
function useIsAdministrator() {
  const role = useSession().user?.role;
  return role === 'SuperUser' || role === 'Administracion';
}

export function InvitadoCard({
  invitado,
  direccionId,
  onDireccion,
  onRemoved,
  canEdit,
}: {
  invitado: Invitado;
  direccionId: number | null;
  onDireccion: (id: number) => void;
  onRemoved: () => void;
  canEdit: boolean;
}) {
  const isAdmin = useIsAdministrator();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState(false);
  const queryClient = useQueryClient();
  const notify = useNotify();
  const ask = useConfirm();
  const [removing, setRemoving] = useState(false);

  async function remove() {
    const pedidos = await pedidosApi.list({ invitadoId: invitado.id });
    const extra = pedidos.length ? ` y sus ${pedidos.length} pedido${pedidos.length === 1 ? '' : 's'}` : '';
    if (
      !(await ask(`¿Eliminar a ${invitado.name}${extra}?`, {
        detail: 'Se dan de baja; sus lugares en la agenda quedan libres.',
        confirmText: 'Eliminar',
        danger: true,
      }))
    )
      return;
    setRemoving(true);
    try {
      await invitadosApi.remove(invitado.id);
      await queryClient.invalidateQueries({ queryKey: ['invitados'] });
      await queryClient.invalidateQueries({ queryKey: keys.pedidos });
      await queryClient.invalidateQueries({ queryKey: ['agenda'] });
      notify(`${invitado.name} eliminado.`);
      onRemoved();
    } catch (e) {
      notify((e as Error).message, true);
    } finally {
      setRemoving(false);
    }
  }

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>{invitado.name}</h2>
          <p className="muted">
            WhatsApp {invitado.whatsAppPhone}
            {invitado.howFoundUs && ` · ${invitado.howFoundUs}`}
          </p>
        </div>
        <div className="actions tight">
          {canEdit && (
            <button type="button" className="small" onClick={() => setEditing(true)}>
              Editar
            </button>
          )}
          {isAdmin && (
            <button type="button" className="small danger" disabled={removing} onClick={() => void remove()}>
              Eliminar
            </button>
          )}
        </div>
      </div>
      <div className="card__body stack">
        {invitado.addresses.length === 0 && <div className="alert">No tiene direcciones. Agrega una para agendar.</div>}
        {invitado.addresses.map((d) => (
          <DireccionOption key={d.id} invitadoId={invitado.id} direccion={d} selected={d.id === direccionId} onSelect={() => onDireccion(d.id)} />
        ))}
        {canEdit && (
          <div>
            <button type="button" className="small" onClick={() => setAdding(true)}>
              Agregar dirección
            </button>
          </div>
        )}
      </div>
      {adding && <DireccionForm invitadoId={invitado.id} onClose={() => setAdding(false)} />}
      {editing && <InvitadoForm invitado={invitado} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
    </section>
  );
}

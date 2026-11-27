import { type Schemas } from '../../../shared/api/client';
import { ACCOUNT } from '../types/crm';
import { useAction } from '../../../shared/hooks/useSave';
import { keys } from '../../../shared/api/queryKeys';
import { formatDateTime } from '../../../shared/utils/dates';
import { invitadosApi } from '../../invitados';

/** State of the invitado's app account, last access, "Invitar a la app" and blocking the access. */
export function AppAccount({ ficha, canEdit }: { ficha: Schemas['CrmFichaDto']; canEdit: boolean }) {
  const { account } = ficha;
  const id = ficha.invitado.id;
  const invite = useAction(
    async () => {
      // The office sends the invitation from its own WhatsApp; the API returns the text and records the date.
      const r = await invitadosApi.invitarApp(id);
      window.open(r.whatsAppUrl, '_blank', 'noreferrer');
    },
    [keys.crm],
    'Invitación lista en WhatsApp.',
  );
  const block = useAction(
    (blocked: boolean) => invitadosApi.bloquearApp(id, { blocked }),
    [keys.crm],
    'Acceso a la app actualizado.',
  );
  const hasUser = account.state === 'Activa' || account.state === 'Bloqueada';

  return (
    <section className="stack">
      <h3>Cuenta de la app</h3>
      <div className="actions" style={{ justifyContent: 'space-between' }}>
        <span>
          <span className={`badge ${ACCOUNT[account.state].tone}`}>{ACCOUNT[account.state].name}</span>
          {account.lastAccess && <span className="muted"> · último acceso {formatDateTime(account.lastAccess)}</span>}
          {!hasUser && account.invitedAt && <span className="muted"> · invitado el {formatDateTime(account.invitedAt)}</span>}
        </span>
        {canEdit && (
          <div className="actions tight">
            {!hasUser && !ficha.crm.doNotContact && (
              <button type="button" className="small" disabled={invite.isPending} onClick={() => invite.mutate(undefined)}>
                {account.invitedAt ? 'Invitar de nuevo' : 'Invitar a la app'}
              </button>
            )}
            {account.state === 'Activa' && (
              <button
                type="button"
                className="small danger"
                disabled={block.isPending}
                onClick={() => confirm('¿Bloquear el acceso a la app? Se cierran sus sesiones abiertas.') && block.mutate(true)}
              >
                Bloquear acceso
              </button>
            )}
            {account.state === 'Bloqueada' && (
              <button type="button" className="small" disabled={block.isPending} onClick={() => block.mutate(false)}>
                Desbloquear
              </button>
            )}
          </div>
        )}
      </div>
      <span className="muted">
        Avisos: push {ficha.invitado.receivesPush ? 'sí' : 'no'} · WhatsApp de servicio {ficha.invitado.receivesWhatsApp ? 'sí' : 'no'} · promociones
        por WhatsApp {ficha.invitado.receivesPromotions ? 'sí' : 'no'}
        {ficha.crm.doNotContact && ' · no contactar'}
      </span>
    </section>
  );
}

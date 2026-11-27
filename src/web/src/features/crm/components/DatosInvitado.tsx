import { useState } from 'react';
import { type Schemas } from '../../../shared/api/client';
import { useSession } from '../../../shared/session/Session';
import { TIPO } from '../types/crm';
import { InvitadoForm } from '../../invitados';

const yesNo = (v: boolean) => (v ? 'Sí' : 'No');

/** Datos tab of the ficha: the invitado's record and notice preferences; "Editar" opens the same form as Agendar. */
export function DatosInvitado({ ficha }: { ficha: Schemas['CrmFichaDto'] }) {
  const i = ficha.invitado;
  // Editing the invitado is an Agendar permission in the API (PUT /invitados/{id}).
  const canEdit = useSession().can('agendar', 'edit');
  const [editing, setEditing] = useState(false);
  const rows: [string, string | null | undefined][] = [
    ['Nombre', i.name],
    ['WhatsApp', i.whatsAppPhone],
    ['Otro teléfono', i.otherPhone],
    ['Correo', i.email],
    ['Tipo', TIPO[i.type]],
    ['Cómo nos conoció', i.howFoundUs],
    ['Campaña', i.campaign],
    ['Observaciones', i.notes],
  ];

  return (
    <section className="stack">
      <div className="actions" style={{ justifyContent: 'space-between' }}>
        <h3>Datos</h3>
        {canEdit && (
          <button type="button" className="small" onClick={() => setEditing(true)}>
            Editar
          </button>
        )}
      </div>
      <dl className="ficha__data">
        {rows.map(([label, value]) => (
          <div key={label}>
            <dt className="muted">{label}</dt>
            <dd>{value || '—'}</dd>
          </div>
        ))}
      </dl>
      <h3>Avisos</h3>
      <dl className="ficha__data">
        <div>
          <dt className="muted">Push en la app</dt>
          <dd>{yesNo(i.receivesPush)}</dd>
        </div>
        <div>
          <dt className="muted">WhatsApp de servicio</dt>
          <dd>{yesNo(i.receivesWhatsApp)}</dd>
        </div>
        <div>
          <dt className="muted">Promociones por WhatsApp</dt>
          <dd>{yesNo(i.receivesPromotions)}</dd>
        </div>
        <div>
          <dt className="muted">No contactar (CRM)</dt>
          <dd>{yesNo(ficha.crm.doNotContact)}</dd>
        </div>
      </dl>
      {editing && <InvitadoForm invitado={i} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
    </section>
  );
}

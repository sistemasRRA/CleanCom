import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { emptyDireccion, type Invitado, INVITADO_TYPES, type InvitadoType, toDireccionRequest } from '../types/invitados';
import { keys } from '../../../shared/api/queryKeys';
import { type Schemas } from '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { DireccionFieldsForm } from './DireccionForm';
import { invitadosApi } from '../services/invitadosApi';
import { operacionApi } from '../../operacion';

/** Capture or edit an invitado (with the first address when new). Also used by Clientes (CRM) for "Nuevo cliente". */
export function InvitadoForm({ invitado, onClose, onSaved }: { invitado?: Invitado; onClose: () => void; onSaved: (i: Invitado) => void }) {
  const [name, setName] = useState(invitado?.name ?? '');
  const [phone, setPhone] = useState(invitado?.whatsAppPhone ?? '');
  const [otherPhone, setOtherPhone] = useState(invitado?.otherPhone ?? '');
  const [email, setEmail] = useState(invitado?.email ?? '');
  const [howFoundUs, setHowFoundUs] = useState(invitado?.howFoundUs ?? '');
  const [campaign, setCampaign] = useState(invitado?.campaign ?? '');
  const [notes, setNotes] = useState(invitado?.notes ?? '');
  const [type, setType] = useState<InvitadoType>(invitado?.type ?? 'ClienteFinal');
  const [receivesWhatsApp, setReceivesWhatsApp] = useState(invitado?.receivesWhatsApp ?? true);
  const [direccion, setDireccion] = useState(emptyDireccion);
  // "Cómo nos conoció" comes from its catalog (Operación › Catálogos › Motivos).
  const howFoundUsOptions = useQuery({
    queryKey: [...keys.reasons, 'HowFoundUs'],
    queryFn: () => operacionApi.listMotivos({ type: 'HowFoundUs' }),
    staleTime: 5 * 60_000,
  });
  const options = howFoundUsOptions.data?.filter((o) => o.active).map((o) => o.text) ?? [];

  const save = useSave(
    () => {
      const body: Schemas['SaveInvitadoRequest'] = {
        name: name.trim(),
        whatsAppPhone: phone,
        otherPhone: optional(otherPhone),
        email: optional(email),
        type,
        howFoundUs: optional(howFoundUs),
        campaign: optional(campaign),
        notes: optional(notes),
        address: invitado ? null : toDireccionRequest(direccion),
        receivesWhatsApp,
      };
      return invitado ? invitadosApi.update(invitado.id, body) : invitadosApi.create(body);
    },
    { refetch: [['invitados'], keys.crm], message: invitado ? 'Invitado actualizado.' : 'Invitado capturado.', onDone: onSaved },
  );

  return (
    <Dialog
      title={invitado ? `Editar a ${invitado.name}` : 'Nuevo invitado'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Nombre" wide>
          <input value={name} onChange={(e) => setName(e.target.value)} required autoFocus />
        </Field>
        <Field label="WhatsApp" hint="10 dígitos">
          <input value={phone} onChange={(e) => setPhone(e.target.value)} inputMode="tel" required />
        </Field>
        <Field label="Otro teléfono">
          <input value={otherPhone} onChange={(e) => setOtherPhone(e.target.value)} inputMode="tel" />
        </Field>
        <Field label="Correo">
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </Field>
        <Field label="Tipo">
          <select value={type} onChange={(e) => setType(e.target.value as InvitadoType)}>
            {INVITADO_TYPES.filter((t) => t.key !== 'TiendaInterna' || type === 'TiendaInterna').map((t) => (
              <option key={t.key} value={t.key}>
                {t.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Cómo nos conoció">
          <select value={howFoundUs} onChange={(e) => setHowFoundUs(e.target.value)}>
            <option value="">Elige una opción</option>
            {/* An invitado captured before the catalog keeps its text. */}
            {howFoundUs && !options.includes(howFoundUs) && <option value={howFoundUs}>{howFoundUs}</option>}
            {options.map((o) => (
              <option key={o} value={o}>
                {o}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Campaña">
          <input value={campaign} onChange={(e) => setCampaign(e.target.value)} />
        </Field>
        <Field label="Observaciones" wide>
          <input value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>
        <Checkbox wide checked={receivesWhatsApp} onChange={setReceivesWhatsApp}>
          Recibe avisos por WhatsApp
        </Checkbox>
      </div>
      {!invitado && (
        <>
          <h3>Dirección</h3>
          <DireccionFieldsForm value={direccion} onChange={setDireccion} />
        </>
      )}
    </Dialog>
  );
}

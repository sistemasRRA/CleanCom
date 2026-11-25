import { useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { addressLine, type Direccion, type DireccionFields, emptyDireccion, toDireccionFields, toDireccionRequest } from '../types/invitados';
import { ZonaPicker } from './ZonaPicker';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { Field } from '../../../shared/components/Form';
import { AddressLocation } from '../../../shared/components/AddressLocation';
import { useSave } from '../../../shared/hooks/useSave';
import { Dialog } from '../../../shared/components/Dialog';
import { useSession } from '../../../shared/session/Session';
import { invitadosApi } from '../services/invitadosApi';
import { operacionApi } from '../../operacion';

export function DireccionOption({
  invitadoId,
  direccion: d,
  selected,
  onSelect,
}: {
  invitadoId: number;
  direccion: Direccion;
  selected: boolean;
  onSelect: () => void;
}) {
  return (
    <div className={`address-option${selected ? ' selected' : ''}`}>
      <label>
        <input type="radio" checked={selected} onChange={onSelect} />
        <span>
          {d.label && <strong>{d.label}: </strong>}
          {addressLine(d)}
        </span>
      </label>
      <div className="actions tight">
        <ZonaPicker invitadoId={invitadoId} address={d} />
        <EditDireccionButton invitadoId={invitadoId} direccion={d} />
      </div>
    </div>
  );
}

export function DireccionFieldsForm({ value, onChange }: { value: DireccionFields; onChange: (v: DireccionFields) => void }) {
  const set = (k: keyof DireccionFields) => (e: { target: { value: string } }) => onChange({ ...value, [k]: e.target.value });

  // Official catalog: the postal code suggests its colonias and fills the alcaldía.
  const cp = value.postalCode.trim();
  const colonias = useQuery({
    queryKey: [...keys.postalCodes, cp],
    queryFn: () => operacionApi.postalCode(cp),
    enabled: /^\d{5}$/.test(cp),
    staleTime: Infinity,
  });
  const options = colonias.data ?? [];
  useEffect(() => {
    if (options.length === 0) return;
    const match = options.find((o) => o.settlement.toLowerCase() === value.neighborhood.trim().toLowerCase());
    const only = options.length === 1 ? options[0] : undefined;
    const neighborhood = value.neighborhood || only?.settlement || '';
    const borough = (match ?? only ?? options[0]).borough;
    if (neighborhood !== value.neighborhood || borough !== value.borough) onChange({ ...value, neighborhood, borough });
    // Only when the catalog answers or the colonia changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [colonias.data, value.neighborhood]);

  // The map answers after a request: write on top of the latest fields, not the ones of that render.
  const latest = useRef(value);
  latest.current = value;

  return (
    <div className="form-grid">
      <Field label="Calle" wide>
        <input value={value.street} onChange={set('street')} required />
      </Field>
      <Field label="Número">
        <input value={value.exteriorNumber} onChange={set('exteriorNumber')} />
      </Field>
      <Field label="Interior">
        <input value={value.interiorNumber} onChange={set('interiorNumber')} />
      </Field>
      <Field
        label="Código postal"
        hint={
          colonias.data && /^\d{5}$/.test(cp)
            ? options.length
              ? `${options.length} colonia${options.length === 1 ? '' : 's'} en el catálogo oficial`
              : 'No está en el catálogo oficial'
            : undefined
        }
      >
        <input
          value={value.postalCode}
          // Another postal code means other colonias: the chosen colonia and alcaldía start over.
          onChange={(e) =>
            onChange({ ...value, postalCode: e.target.value, ...(e.target.value !== value.postalCode ? { neighborhood: '', borough: '' } : {}) })
          }
          inputMode="numeric"
          maxLength={5}
          required
        />
      </Field>
      <Field label="Colonia">
        {colonias.data && options.length === 0 ? (
          // Postal code not in the official catalog: the colonia is typed by hand.
          <input value={value.neighborhood} onChange={set('neighborhood')} autoComplete="off" />
        ) : (
          <select value={value.neighborhood} onChange={set('neighborhood')} disabled={options.length === 0}>
            <option value="">{options.length ? 'Elige la colonia' : 'Captura el código postal'}</option>
            {/* An address saved with a colonia that is not in the catalog keeps it. */}
            {value.neighborhood && !options.some((o) => o.settlement === value.neighborhood) && (
              <option value={value.neighborhood}>{value.neighborhood}</option>
            )}
            {options.map((o) => (
              <option key={`${o.settlement}-${o.borough}`} value={o.settlement}>
                {o.settlement} ({o.settlementType})
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label="Alcaldía">
        <input value={value.borough} onChange={set('borough')} />
      </Field>
      <Field label="Etiqueta">
        <input value={value.label} onChange={set('label')} placeholder="Casa" />
      </Field>
      <AddressLocation
        label="Ubicación (latitud, longitud)"
        placeholder="19.3608, -99.1968"
        hint="Mueve el pin o haz clic en el mapa donde está la casa. Decide la zona; sin ella se usa el código postal."
        query={value}
        canLocate={value.street.trim() !== '' && /^\d{5}$/.test(cp)}
        value={value.coordinates}
        onChange={(coordinates) => onChange({ ...latest.current, coordinates })}
      />
      <Field label="Referencias" wide>
        <input value={value.references} onChange={set('references')} />
      </Field>
    </div>
  );
}

/**
 * Add an address, or edit a saved one (`direccion`): data and point on the map. The API locates the zona again;
 * a zona chosen by hand is kept (it is changed with "Cambiar").
 */
export function DireccionForm({ invitadoId, direccion, onClose }: { invitadoId: number; direccion?: Direccion; onClose: () => void }) {
  const [fields, setFields] = useState(() => (direccion ? toDireccionFields(direccion) : emptyDireccion));
  const save = useSave(
    () =>
      direccion
        ? invitadosApi.updateDireccion(invitadoId, direccion.id, {
            ...toDireccionRequest(fields),
            zonaId: direccion.zonaAssignedManually ? direccion.zonaId : null,
          })
        : invitadosApi.addDireccion(invitadoId, toDireccionRequest(fields)),
    {
      refetch: [['invitados'], ['agenda'], keys.crm],
      message: direccion ? 'Dirección actualizada.' : 'Dirección agregada.',
      onDone: onClose,
    },
  );
  return (
    <Dialog
      title={direccion ? 'Editar dirección' : 'Agregar dirección'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <DireccionFieldsForm value={fields} onChange={setFields} />
    </Dialog>
  );
}

/** "Editar" of a saved address, for whoever edits Agendar (also in the CRM ficha). */
export function EditDireccionButton({ invitadoId, direccion }: { invitadoId: number; direccion: Direccion }) {
  const canEdit = useSession().can('agendar', 'edit');
  const [open, setOpen] = useState(false);
  if (!canEdit) return null;
  return (
    <>
      <button type="button" className="small" onClick={() => setOpen(true)}>
        Editar
      </button>
      {open && <DireccionForm invitadoId={invitadoId} direccion={direccion} onClose={() => setOpen(false)} />}
    </>
  );
}

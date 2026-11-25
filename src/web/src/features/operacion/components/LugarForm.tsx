import { useState } from 'react';
import { type Lugar, LUGAR_TYPES } from '../types/catalogs';
import { useSave } from '../../../shared/hooks/useSave';
import { AddressLocation, parsePoint } from '../../../shared/components/AddressLocation';
import { optional } from '../../../shared/utils/format';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function LugarForm({ lugar, onClose }: { lugar: Lugar | null; onClose: () => void }) {
  const [f, setF] = useState({
    name: lugar?.name ?? '',
    type: lugar?.type ?? ('Tienda' as Lugar['type']),
    address: lugar?.address ?? '',
    // One "latitud, longitud" field with the map instead of two boxes.
    coordinates: lugar?.lat != null && lugar?.lng != null ? `${lugar.lat}, ${lugar.lng}` : '',
    openingHours: lugar?.openingHours ?? '',
    active: lugar?.active ?? true,
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const save = useSave(
    () => {
      const point = parsePoint(f.coordinates);
      if (f.coordinates.trim() !== '' && !point) {
        return Promise.reject(new Error('La ubicación no es válida: usa "latitud, longitud", el link completo de Google Maps o el pin del mapa.'));
      }
      const body = {
        name: f.name.trim(),
        type: f.type,
        address: f.address.trim(),
        lat: point?.lat ?? null,
        lng: point?.lng ?? null,
        openingHours: optional(f.openingHours),
        active: f.active,
      };
      return lugar ? operacionApi.updateLugar(lugar.id, body) : operacionApi.createLugar(body);
    },
    { refetch: [keys.lugares], message: lugar ? 'Lugar actualizado.' : 'Lugar creado.', onDone: onClose },
  );

  return (
    <Dialog title={lugar?.name ?? 'Nuevo lugar'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Nombre">
          <input autoFocus value={f.name} onChange={(e) => change({ name: e.target.value })} required />
        </Field>
        <Field label="Tipo">
          <select value={f.type} onChange={(e) => change({ type: e.target.value as Lugar['type'] })}>
            {Object.entries(LUGAR_TYPES).map(([key, name]) => (
              <option key={key} value={key}>
                {name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Dirección" wide>
          <input value={f.address} onChange={(e) => change({ address: e.target.value })} required />
        </Field>
        <AddressLocation
          label="Ubicación (latitud, longitud)"
          placeholder="19.36, -99.19"
          hint="Mueve el pin o haz clic en el mapa donde está el lugar. También acepta el link completo de Google Maps."
          query={{ street: f.address }}
          canLocate={f.address.trim().length >= 8}
          value={f.coordinates}
          onChange={(coordinates) => change({ coordinates })}
        />
        <Field label="Horario" wide>
          <input value={f.openingHours} onChange={(e) => change({ openingHours: e.target.value })} placeholder="L a V 9:00 a 19:00" />
        </Field>
        <Checkbox wide checked={f.active} onChange={(active) => change({ active })}>
          Activo
        </Checkbox>
      </div>
    </Dialog>
  );
}

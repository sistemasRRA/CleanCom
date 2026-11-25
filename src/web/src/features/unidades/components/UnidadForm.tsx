import { useState } from 'react';
import type { Detail } from '../types/unidades';
import { type Schemas } from '../../../shared/api/client';
import { optional, optionalNumber } from '../../../shared/utils/format';
import { useSave } from '../../../shared/hooks/useSave';
import { listKey } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { LABEL as TYPE_LABEL } from './UnidadIcon';
import { unidadesApi } from '../services/unidadesApi';

export function UnidadForm({ detail, onClose, onSaved }: { detail?: Detail; onClose: () => void; onSaved: (d: Detail) => void }) {
  const c = detail?.card;
  const [f, setF] = useState({
    plate: c?.plate ?? '',
    brand: c?.brand ?? '',
    model: c?.model ?? '',
    year: String(c?.year ?? new Date().getFullYear()),
    km: String(c?.km ?? 0),
    garmentCapacity: detail?.garmentCapacity != null ? String(detail.garmentCapacity) : '',
    serviceEveryKm: detail?.serviceEveryKm != null ? String(detail.serviceEveryKm) : '',
    serviceEveryMonths: detail?.serviceEveryMonths != null ? String(detail.serviceEveryMonths) : '',
    outOfService: c?.state === 'FueraDeServicio',
    verificationExempt: c?.verificationExempt ?? false,
    type: c?.type ?? ('Otro' as Schemas['UnidadType']),
    notes: detail?.notes ?? '',
    active: c?.active ?? true,
  });
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const body: Schemas['SaveUnidadRequest'] = {
    plate: f.plate,
    brand: f.brand,
    model: f.model,
    year: Number(f.year),
    km: Number(f.km),
    garmentCapacity: optionalNumber(f.garmentCapacity),
    serviceEveryKm: optionalNumber(f.serviceEveryKm),
    serviceEveryMonths: optionalNumber(f.serviceEveryMonths),
    state: f.outOfService ? 'FueraDeServicio' : 'Activa',
    notes: optional(f.notes),
    active: f.active,
    verificationExempt: f.verificationExempt,
    type: f.type,
  };
  const save = useSave(() => (c ? unidadesApi.update(c.id, body) : unidadesApi.create(body)), {
    refetch: [listKey],
    message: c ? 'Unidad guardada.' : 'Unidad agregada.',
    onDone: (d) => onSaved(d as Detail),
  });

  return (
    <Dialog
      title={c ? `Editar ${c.plate}` : 'Nueva unidad'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Placa" hint="Se guarda sin espacios ni guiones.">
          <input value={f.plate} onChange={(e) => set({ plate: e.target.value })} required maxLength={15} />
        </Field>
        <Field label="Tipo">
          <select value={f.type} onChange={(e) => set({ type: e.target.value as Schemas['UnidadType'] })}>
            {(Object.keys(TYPE_LABEL) as Schemas['UnidadType'][]).map((t) => (
              <option key={t} value={t}>
                {TYPE_LABEL[t]}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Marca">
          <input value={f.brand} onChange={(e) => set({ brand: e.target.value })} required maxLength={60} />
        </Field>
        <Field label="Modelo">
          <input value={f.model} onChange={(e) => set({ model: e.target.value })} required maxLength={60} />
        </Field>
        <Field label="Año">
          <input type="number" value={f.year} onChange={(e) => set({ year: e.target.value })} required min={1980} max={2100} />
        </Field>
        <Field label="Kilometraje">
          <input type="number" value={f.km} onChange={(e) => set({ km: e.target.value })} required min={0} />
        </Field>
        <Field label="Capacidad de prendas" hint="Vacío: la de Operación.">
          <input type="number" value={f.garmentCapacity} onChange={(e) => set({ garmentCapacity: e.target.value })} min={1} />
        </Field>
        <Field label="Servicio cada (km)" hint="Vacío: el de Operación.">
          <input type="number" value={f.serviceEveryKm} onChange={(e) => set({ serviceEveryKm: e.target.value })} min={1} />
        </Field>
        <Field label="Servicio cada (meses)" hint="Vacío: el de Operación.">
          <input type="number" value={f.serviceEveryMonths} onChange={(e) => set({ serviceEveryMonths: e.target.value })} min={1} max={60} />
        </Field>
        <Field label="Notas" wide>
          <textarea value={f.notes} onChange={(e) => set({ notes: e.target.value })} maxLength={1000} rows={2} />
        </Field>
        <Checkbox checked={f.outOfService} onChange={(v) => set({ outOfService: v })} wide>
          Fuera de servicio
        </Checkbox>
        <Checkbox checked={f.verificationExempt} onChange={(v) => set({ verificationExempt: v })} wide>
          Exenta de verificación (por ejemplo, las trimotos)
        </Checkbox>
        {c && (
          <Checkbox checked={!f.active} onChange={(v) => set({ active: !v })} wide>
            Dar de baja (deja de aparecer; no se borra)
          </Checkbox>
        )}
      </div>
    </Dialog>
  );
}

import { useState } from 'react';
import type { Coverage } from '../types/catalogs';
import { type Schemas } from '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function CoverageForm({ coverage, zonas, onClose }: { coverage: Coverage | null; zonas: Schemas['ZonaDto'][]; onClose: () => void }) {
  const [f, setF] = useState({
    postalCode: coverage?.postalCode ?? '',
    neighborhood: coverage?.neighborhood ?? '',
    borough: coverage?.borough ?? '',
    zonaId: coverage?.zonaId.toString() ?? '',
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const save = useSave(
    () => {
      const body = {
        postalCode: f.postalCode.trim(),
        neighborhood: f.neighborhood.trim(),
        borough: f.borough.trim(),
        zonaId: Number(f.zonaId),
      };
      return coverage ? operacionApi.updateCobertura(coverage.id, body) : operacionApi.createCobertura(body);
    },
    { refetch: [keys.coverage], message: 'Cobertura guardada.', onDone: onClose },
  );
  return (
    <Dialog
      title={coverage ? 'Editar cobertura' : 'Agregar cobertura'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Código postal">
          <input autoFocus inputMode="numeric" maxLength={5} value={f.postalCode} onChange={(e) => change({ postalCode: e.target.value })} required />
        </Field>
        <Field label="Zona">
          <select value={f.zonaId} onChange={(e) => change({ zonaId: e.target.value })} required>
            <option value="">Elige…</option>
            {zonas
              .filter((z) => z.active)
              .map((z) => (
                <option key={z.id} value={z.id}>
                  {z.code}
                </option>
              ))}
          </select>
        </Field>
        <Field label="Colonia" wide>
          <input value={f.neighborhood} onChange={(e) => change({ neighborhood: e.target.value })} required />
        </Field>
        <Field label="Alcaldía" wide>
          <input value={f.borough} onChange={(e) => change({ borough: e.target.value })} required />
        </Field>
      </div>
    </Dialog>
  );
}

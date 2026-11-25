import { useState } from 'react';
import { day, type Detail, DOCUMENT, type DocumentKind, EVENT } from '../types/unidades';
import { useSave } from '../../../shared/hooks/useSave';
import { unidadesApi } from '../services/unidadesApi';
import { detailKey, listKey } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';

/** A document, or with `photo` only the unidad's picture (an image, required). */
export function DocumentForm({ detail, photo = false, onClose }: { detail: Detail; photo?: boolean; onClose: () => void }) {
  const id = detail.card.id;
  const [f, setF] = useState({ kind: (photo ? 'Foto' : 'Seguro') as DocumentKind, validFrom: '', validUntil: '', description: '', eventId: '' });
  const [file, setFile] = useState<File | null>(null);
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const save = useSave(
    () => {
      // Multipart: the form data is built here, with the file.
      const data = new FormData();
      data.append('Kind', f.kind);
      if (f.validFrom) data.append('ValidFrom', f.validFrom);
      if (f.validUntil) data.append('ValidUntil', f.validUntil);
      if (f.description.trim()) data.append('Description', f.description.trim());
      if (f.eventId) data.append('EventId', f.eventId);
      if (file) data.append('File', file);
      return unidadesApi.addDocument(id, data);
    },
    { refetch: [listKey, detailKey(id)], message: photo ? 'Foto guardada.' : 'Documento agregado.', onDone: onClose },
  );
  const needsUntil = f.kind === 'Seguro' || f.kind === 'TarjetaCirculacion';
  if (photo) {
    return (
      <Dialog
        title={`Foto de ${detail.card.plate}`}
        onClose={onClose}
        onSubmit={() => save.mutate(undefined)}
        busy={save.isPending}
        error={save.error}
      >
        <Field label="Foto" hint="JPG, PNG o WebP, hasta 15 MB. Se ve en la tarjeta de la unidad.">
          <input type="file" accept="image/jpeg,image/png,image/webp" required onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </Field>
      </Dialog>
    );
  }
  return (
    <Dialog
      title={`Documento de ${detail.card.plate}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Tipo">
          <select value={f.kind} onChange={(e) => set({ kind: e.target.value as DocumentKind })}>
            {(Object.keys(DOCUMENT) as DocumentKind[])
              .filter((k) => k !== 'Foto')
              .map((k) => (
                <option key={k} value={k}>
                  {DOCUMENT[k]}
                </option>
              ))}
          </select>
        </Field>
        <Field label={f.kind === 'Verificacion' ? 'Fecha de la verificación' : 'Vigente desde'}>
          <input type="date" value={f.validFrom} onChange={(e) => set({ validFrom: e.target.value })} required={f.kind === 'Verificacion'} />
        </Field>
        {f.kind !== 'Verificacion' && (
          <Field label="Vigente hasta">
            <input type="date" value={f.validUntil} onChange={(e) => set({ validUntil: e.target.value })} required={needsUntil} />
          </Field>
        )}
        {f.kind === 'Comprobante' && detail.events.length > 0 && (
          <Field label="Del registro">
            <select value={f.eventId} onChange={(e) => set({ eventId: e.target.value })}>
              <option value="">Ninguno</option>
              {detail.events.map((e) => (
                <option key={e.id} value={e.id}>
                  {day(e.date)} · {EVENT[e.kind]} · {e.description}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Descripción" wide>
          <input
            value={f.description}
            onChange={(e) => set({ description: e.target.value })}
            maxLength={300}
            placeholder="Aseguradora, número de póliza…"
          />
        </Field>
        <Field label="Archivo" hint="PDF, JPG, PNG o WebP, hasta 15 MB. Opcional." wide>
          <input type="file" accept="application/pdf,image/jpeg,image/png,image/webp" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </Field>
      </div>
    </Dialog>
  );
}

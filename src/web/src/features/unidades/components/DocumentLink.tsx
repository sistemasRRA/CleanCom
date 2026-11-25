import { useState } from 'react';
import '../../../shared/api/client';
import { unidadesApi } from '../services/unidadesApi';

/** Opens a document with the session (the file is behind the API's permissions). */
export function DocumentLink({ unidadId, documentoId }: { unidadId: number; documentoId: number }) {
  const [busy, setBusy] = useState(false);
  async function open() {
    setBusy(true);
    try {
      const blob = (await unidadesApi.documentFile(unidadId, documentoId)) as Blob;
      window.open(URL.createObjectURL(blob), '_blank', 'noopener');
    } finally {
      setBusy(false);
    }
  }
  return (
    <button type="button" className="small" disabled={busy} onClick={() => void open()}>
      {busy ? 'Abriendo…' : 'Ver archivo'}
    </button>
  );
}

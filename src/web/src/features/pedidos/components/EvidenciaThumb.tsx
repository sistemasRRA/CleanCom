import { useQuery } from '@tanstack/react-query';
import { type Schemas } from '../../../shared/api/client';
import { Loader } from '../../../shared/components/Loader';
import { pedidosApi } from '../services/pedidosApi';

/** Photo or signature of a visit; the file needs the session, so it is fetched and shown as a local URL. */
export function EvidenciaThumb({ id, kind, distance }: { id: number; kind: Schemas['EvidenceKind']; distance?: number | null }) {
  const file = useQuery({
    queryKey: ['evidencias', id],
    queryFn: async () => URL.createObjectURL((await pedidosApi.evidenciaFile(id)) as Blob),
    staleTime: Infinity,
  });
  const label = kind === 'Firma' ? 'Firma' : `Foto${distance != null ? ` · a ${distance} m` : ''}`;
  return (
    <a href={file.data} target="_blank" rel="noreferrer" className={`evidencia${kind === 'Firma' ? ' firma' : ''}`} title={label}>
      {file.data ? (
        <img src={file.data} alt={label} />
      ) : file.isError ? (
        <span className="muted">No disponible</span>
      ) : (
        <Loader size="small" label="" />
      )}
      <span>{label}</span>
    </a>
  );
}

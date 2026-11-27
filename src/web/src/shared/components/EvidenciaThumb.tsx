import { useQuery } from '@tanstack/react-query';
import { type Schemas } from '../api/client';
import { evidenciasFilesApi } from '../api/evidenciasApi';
import { Loader } from './Loader';

/**
 * Photo or signature of a visit; the file needs the session, so it is fetched and shown as a local URL. A photo
 * taken farther than Gps.ToleranciaMetros from the parada is marked (`outside`).
 */
export function EvidenciaThumb({
  id,
  kind,
  distance,
  outside,
  small,
}: {
  id: number;
  kind: Schemas['EvidenceKind'];
  distance?: number | null;
  outside?: boolean;
  small?: boolean;
}) {
  const file = useQuery({
    queryKey: ['evidencias', id],
    queryFn: async () => URL.createObjectURL((await evidenciasFilesApi.file(id)) as Blob),
    staleTime: Infinity,
  });
  const label = kind === 'Firma' ? 'Firma' : `Foto${distance != null ? ` · a ${distance} m` : ''}`;
  const className = ['evidencia', kind === 'Firma' && 'firma', outside && 'outside', small && 'small'].filter(Boolean).join(' ');
  return (
    <a href={file.data} target="_blank" rel="noreferrer" className={className} title={outside ? `${label}: fuera de radio` : label}>
      {file.data ? (
        <img src={file.data} alt={label} />
      ) : file.isError ? (
        <span className="muted">No disponible</span>
      ) : (
        <Loader size="small" label="" />
      )}
      <span>{small ? (kind === 'Firma' ? 'Firma' : distance != null ? `${distance} m` : 'Foto') : label}</span>
    </a>
  );
}

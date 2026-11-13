import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap } from '../../api/client';
import { keys } from '../../api/queries';
import { useSession } from '../../session/Session';
import { QueryState, formatDateTime, useSave } from '../../components/Common';

const OFFICIAL_URL = 'https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx';

/**
 * Official postal code catalog. It fills colonia and alcaldía when capturing an address; it does not decide the zona
 * (that is Cobertura). Only SuperUser and Administración import it.
 */
export function PostalCodes() {
  const role = useSession().user?.role;
  const isAdmin = role === 'SuperUser' || role === 'Administracion';
  const summary = useQuery({ queryKey: keys.postalCodes, queryFn: () => unwrap(api.GET('/codigos-postales/resumen')) });
  const [file, setFile] = useState<File | null>(null);
  const [text, setText] = useState('');
  const [search, setSearch] = useState('');
  useEffect(() => {
    const t = setTimeout(() => setSearch(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);
  const results = useQuery({
    queryKey: [...keys.postalCodes, 'search', search],
    queryFn: () => unwrap(api.GET('/codigos-postales', { params: { query: { search } } })),
    enabled: search.length >= 2,
  });

  const upload = useSave(
    (f: File) =>
      unwrap(
        api.POST('/codigos-postales/importar', {
          // Multipart: the generated type describes the file as a string.
          body: { file: f as unknown as string },
          bodySerializer: (body) => {
            const data = new FormData();
            data.append('file', body.file as unknown as Blob);
            return data;
          },
        }),
      ),
    {
      refetch: [keys.postalCodes],
      onDone: () => setFile(null),
    },
  );
  const r = upload.data;

  return (
    <div className="stack">
      <section className="card">
        <div className="card__header">
          <div>
            <h2>Códigos postales</h2>
            <p className="muted">
              Catálogo oficial de Correos de México. Al capturar una dirección, el código postal llena la colonia y la alcaldía. La
              zona la decide Cobertura, no este catálogo.
            </p>
          </div>
        </div>
        <div className="card__body stack">
          <QueryState loading={summary.isPending} error={summary.error}>
            {summary.data && summary.data.settlements > 0 ? (
              <div className="actions">
                <span className="badge brand">
                  {summary.data.postalCodes.toLocaleString('es-MX')} códigos postales · {summary.data.settlements.toLocaleString('es-MX')} colonias
                </span>
                {summary.data.states.map((s) => (
                  <span key={s.state} className="badge">
                    {s.state}: {s.postalCodes.toLocaleString('es-MX')}
                  </span>
                ))}
                <span className="muted">Actualizado {formatDateTime(summary.data.lastImportAt)}</span>
              </div>
            ) : (
              <div className="alert">Todavía no se ha cargado el catálogo.</div>
            )}
          </QueryState>

          {isAdmin && (
            <form
              className="stack"
              onSubmit={(e) => {
                e.preventDefault();
                if (file) upload.mutate(file);
              }}
            >
              <ol className="muted import-steps">
                <li>
                  Entra a{' '}
                  <a href={OFFICIAL_URL} target="_blank" rel="noreferrer">
                    Correos de México › Descarga de códigos postales
                  </a>
                  .
                </li>
                <li>Elige el estado (Ciudad de México, Estado de México…) o "Todos", el formato <strong>Txt</strong> y descarga.</li>
                <li>Sube aquí el archivo. Puedes repetirlo cuando Correos actualice el catálogo: lo que ya no venga se da de baja.</li>
              </ol>
              <div className="actions">
                <input type="file" accept=".txt,text/plain" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
                <button type="submit" className="primary" disabled={!file || upload.isPending}>
                  {upload.isPending ? 'Importando…' : 'Importar'}
                </button>
              </div>
              {upload.error && <div className="alert error">{upload.error.message}</div>}
              {r && (
                <div className="alert info">
                  {r.states.join(', ')}: {r.read.toLocaleString('es-MX')} colonias leídas; {r.added} nuevas, {r.updated} actualizadas,{' '}
                  {r.deactivated} dadas de baja.
                </div>
              )}
              <small className="muted">
                El catálogo es gratuito para uso particular y no se permite distribuirlo, por eso no se guarda en el repositorio.
              </small>
            </form>
          )}
        </div>
      </section>

      <section className="card">
        <div className="card__header">
          <h2>Buscar</h2>
          <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Código postal o colonia" style={{ maxWidth: 280 }} />
        </div>
        {search.length >= 2 && (
          <QueryState loading={results.isPending} error={results.error}>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>CP</th>
                    <th>Colonia</th>
                    <th>Tipo</th>
                    <th>Alcaldía</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {results.data?.map((p) => (
                    <tr key={`${p.code}-${p.settlement}-${p.borough}`}>
                      <td>
                        <strong>{p.code}</strong>
                      </td>
                      <td>{p.settlement}</td>
                      <td>{p.settlementType}</td>
                      <td>{p.borough}</td>
                      <td>{p.state}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {results.data?.length === 0 && <div className="empty">Sin resultados.</div>}
            </div>
          </QueryState>
        )}
      </section>
    </div>
  );
}

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useCanEdit } from '../hooks/useCanEdit';
import { useConfirm } from '../../../shared/components/Toasts';
import { useZonas } from '../services/queries';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { optional } from '../../../shared/utils/format';
import type { Coverage } from '../types/catalogs';
import { useAction } from '../../../shared/hooks/useSave';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { QueryState } from '../../../shared/components/Page';
import { CoverageForm } from '../components/CoverageForm';
import { operacionApi } from '../services/operacionApi';

export function CoveragePage() {
  const canEdit = useCanEdit();
  const ask = useConfirm();
  const zonas = useZonas();
  const [search, setSearch] = useState('');
  const [zonaId, setZonaId] = useState('');
  const coverage = useQuery({
    queryKey: [...keys.coverage, search, zonaId],
    queryFn: () => operacionApi.listCobertura({ search: optional(search) ?? undefined, zonaId: zonaId ? Number(zonaId) : undefined }),
    placeholderData: (previous) => previous,
  });
  const [editing, setEditing] = useState<Coverage | 'new' | null>(null);
  const remove = useAction((id: number) => operacionApi.removeCobertura(id), [keys.coverage], 'Registro quitado.');

  const pagedCoverage = usePaged(coverage.data, `${search}|${zonaId}`);
  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Cobertura</h2>
          <p className="muted">Código postal y colonia que atiende cada zona.</p>
        </div>
        <div className="actions">
          <input type="search" placeholder="CP, colonia o alcaldía" value={search} onChange={(e) => setSearch(e.target.value)} />
          <select value={zonaId} onChange={(e) => setZonaId(e.target.value)} aria-label="Zona">
            <option value="">Todas las zonas</option>
            {zonas.data?.map((z) => (
              <option key={z.id} value={z.id}>
                {z.code}
              </option>
            ))}
          </select>
          {canEdit && (
            <button type="button" className="primary" onClick={() => setEditing('new')}>
              Agregar
            </button>
          )}
        </div>
      </div>
      <QueryState loading={coverage.isPending} error={coverage.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>CP</th>
                <th>Colonia</th>
                <th>Alcaldía</th>
                <th>Zona</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pagedCoverage.rows.map((c) => (
                <tr key={c.id}>
                  <td className="num">
                    <strong>{c.postalCode}</strong>
                  </td>
                  <td>{c.neighborhood}</td>
                  <td>{c.borough}</td>
                  <td>
                    <span className="badge brand">{c.zonaCode}</span>
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        <button type="button" className="small" onClick={() => setEditing(c)}>
                          Editar
                        </button>
                        <button
                          type="button"
                          className="small danger"
                          disabled={remove.isPending}
                          onClick={async () =>
                            (await ask(`¿Quitar ${c.postalCode} ${c.neighborhood} de la cobertura?`, { confirmText: 'Quitar', danger: true })) &&
                            remove.mutate(c.id)
                          }
                        >
                          Quitar
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {coverage.data?.length === 0 && <div className="empty">Sin resultados.</div>}
        </div>
        <Pagination {...pagedCoverage} />
      </QueryState>
      {editing && <CoverageForm coverage={editing === 'new' ? null : editing} zonas={zonas.data ?? []} onClose={() => setEditing(null)} />}
    </section>
  );
}

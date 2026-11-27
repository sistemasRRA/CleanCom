import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { QueryState } from '../../../shared/components/Page';
import { bitacoraApi } from '../services/bitacoraApi';
import { ENTITIES, PAGE_SIZE } from '../types/bitacora';
import { ChangeLogList } from '../components/ChangeLogList';

/**
 * The general bitácora (Usuarios › Bitácora), SuperUser and Administración only: who changed what in invitados,
 * pedidos, rutas, metas, usuarios and permisos. Filters by kind of record (and its number) and dates.
 */
export function Bitacora() {
  const [entidad, setEntidad] = useState('');
  const [id, setId] = useState('');
  const [desde, setDesde] = useState('');
  const [hasta, setHasta] = useState('');
  const [page, setPage] = useState(1);
  useEffect(() => setPage(1), [entidad, id, desde, hasta]);
  const recordId = entidad && /^\d+$/.test(id) ? Number(id) : undefined;

  const log = useQuery({
    queryKey: ['bitacora', entidad, recordId, desde, hasta, page],
    queryFn: () =>
      bitacoraApi.search({
        entidad: entidad || undefined,
        id: recordId,
        desde: desde || undefined,
        hasta: hasta || undefined,
        pagina: page,
        porPagina: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  return (
    <section className="card">
      <div className="card__header">
        <div className="actions">
          <select value={entidad} onChange={(e) => setEntidad(e.target.value)} style={{ width: 'auto' }}>
            <option value="">Todo</option>
            {ENTITIES.map((e) => (
              <option key={e.key} value={e.key}>
                {e.name}
              </option>
            ))}
          </select>
          {entidad && <input value={id} onChange={(e) => setId(e.target.value)} placeholder="Número" inputMode="numeric" style={{ maxWidth: 110 }} />}
          <label className="actions tight">
            <span className="muted">Desde</span>
            <input type="date" value={desde} onChange={(e) => setDesde(e.target.value)} />
          </label>
          <label className="actions tight">
            <span className="muted">Hasta</span>
            <input type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} />
          </label>
        </div>
      </div>
      <div className="card__body">
        <QueryState loading={log.isPending} error={log.error}>
          {log.data && <ChangeLogList data={log.data} setPage={setPage} showRecord />}
        </QueryState>
      </div>
    </section>
  );
}

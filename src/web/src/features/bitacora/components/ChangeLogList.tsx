import { ACTION, type ChangeLog, type ChangeLogPage } from '../types/bitacora';
import { formatDateTime } from '../../../shared/utils/dates';
import { Pagination } from '../../../shared/components/Pagination';

/** Dates come as ISO from the API; they are shown in Ciudad de México. Other values as they are. */
function value(v: string | null) {
  if (v == null || v === '') return '—';
  return /^\d{4}-\d{2}-\d{2}T/.test(v) ? formatDateTime(v) : v;
}

/** Who changed what: one line per change, with each field before → after. */
export function ChangeLogList({ data, setPage, showRecord }: { data: ChangeLogPage; setPage: (p: number) => void; showRecord?: boolean }) {
  return (
    <>
      {data.rows.length === 0 && <div className="empty">Sin cambios registrados.</div>}
      <ul className="timeline bitacora">
        {data.rows.map((r: ChangeLog) => (
          <li key={r.id}>
            <span>
              <span className={`badge ${ACTION[r.action].tone}`}>{ACTION[r.action].name}</span>{' '}
              <strong>
                {r.entityLabel}
                {showRecord && ` ${r.entityId}`}
              </strong>
              <span className="muted">
                {' '}
                · {formatDateTime(r.at)} · {r.userName ?? 'Sistema'}
              </span>
            </span>
            {r.changes.length > 0 && (
              <ul className="bitacora__fields">
                {r.changes.map((c) => (
                  <li key={c.field}>
                    <span className="muted">{c.label}:</span>{' '}
                    {r.action === 'Cambio' || r.action === 'Baja' || r.action === 'Reactivacion' ? (
                      <>
                        <s className="muted">{value(c.before)}</s> → {value(c.after)}
                      </>
                    ) : (
                      value(c.after)
                    )}
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
      <Pagination page={data.page} pageCount={Math.max(1, Math.ceil(data.rowCount / data.pageSize))} total={data.rowCount} pageSize={data.pageSize} setPage={setPage} />
    </>
  );
}

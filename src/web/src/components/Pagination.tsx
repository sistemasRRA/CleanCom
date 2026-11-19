import { useEffect, useState } from 'react';

export const PAGE_SIZE = 25;

type PageState = { page: number; pageCount: number; total: number; pageSize: number; setPage: (page: number) => void };

/**
 * Pages a list that already came complete from the API. The page goes back to 1 when <paramref name="resetKey"/>
 * changes (a new search or filter) and is clamped when the list shrinks.
 */
export function usePaged<T>(items: readonly T[] | undefined, resetKey?: unknown, pageSize = PAGE_SIZE): PageState & { rows: T[] } {
  const [page, setPage] = useState(1);
  useEffect(() => setPage(1), [resetKey]);
  const total = items?.length ?? 0;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const current = Math.min(page, pageCount);
  const rows = (items ?? []).slice((current - 1) * pageSize, current * pageSize);
  return { rows, page: current, pageCount, total, pageSize, setPage };
}

/** "26–50 de 312 · ‹ Anterior · Página 2 de 13 · Siguiente ›". Hidden when everything fits in one page. */
export function Pagination({ page, pageCount, total, pageSize, setPage }: PageState) {
  if (total <= pageSize) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);
  return (
    <nav className="pagination" aria-label="Paginado">
      <span className="muted">
        {from}–{to} de {total}
      </span>
      <div className="actions tight">
        <button type="button" className="small" disabled={page <= 1} onClick={() => setPage(1)} title="Primera página">
          «
        </button>
        <button type="button" className="small" disabled={page <= 1} onClick={() => setPage(page - 1)}>
          ‹ Anterior
        </button>
        <span className="pagination__page">
          Página {page} de {pageCount}
        </span>
        <button type="button" className="small" disabled={page >= pageCount} onClick={() => setPage(page + 1)}>
          Siguiente ›
        </button>
        <button type="button" className="small" disabled={page >= pageCount} onClick={() => setPage(pageCount)} title="Última página">
          »
        </button>
      </div>
    </nav>
  );
}

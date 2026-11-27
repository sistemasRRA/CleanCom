import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { HISTORIA, type HistoriaKind } from '../types/crm';
import { keys } from '../../../shared/api/queryKeys';
import { QueryState } from '../../../shared/components/Page';
import { formatDateTime } from '../../../shared/utils/dates';
import { money } from '../../../shared/utils/format';
import { invitadosApi } from '../../invitados';

const SHOWN = 30;

/** Pedidos, visits, tickets, avisos and contacts of the invitado, newest first, filtered by kind. */
export function Historia({ invitadoId }: { invitadoId: number }) {
  const [kind, setKind] = useState<HistoriaKind | null>(null);
  const [all, setAll] = useState(false);
  const historia = useQuery({
    queryKey: [...keys.crm, 'historia', invitadoId],
    queryFn: () => invitadosApi.historia(invitadoId),
  });
  const items = (historia.data ?? []).filter((i) => kind === null || i.kind === kind);
  const shown = all ? items : items.slice(0, SHOWN);

  return (
    <section className="stack">
      <div className="actions" style={{ justifyContent: 'space-between' }}>
        <h3>Historia</h3>
        <div className="segmented">
          <button type="button" className={`segmented__option${kind === null ? ' selected' : ''}`} onClick={() => setKind(null)}>
            Todo
          </button>
          {(Object.keys(HISTORIA) as HistoriaKind[]).map((k) => (
            <button key={k} type="button" className={`segmented__option${kind === k ? ' selected' : ''}`} onClick={() => setKind(k)}>
              {HISTORIA[k]}
            </button>
          ))}
        </div>
      </div>
      <QueryState loading={historia.isPending} error={historia.error}>
        {items.length === 0 && <span className="muted">Sin movimientos.</span>}
        <ul className="timeline">
          {shown.map((i, n) => (
            <li key={n}>
              <strong>
                {i.text}
                {i.amount != null && ` · ${money(i.amount)}`}
              </strong>
              <span className="muted">{formatDateTime(i.at)}</span>
              {i.detail && <span>{i.detail}</span>}
            </li>
          ))}
        </ul>
        {!all && items.length > SHOWN && (
          <button type="button" className="link" onClick={() => setAll(true)}>
            Ver los {items.length - SHOWN} anteriores
          </button>
        )}
      </QueryState>
    </section>
  );
}

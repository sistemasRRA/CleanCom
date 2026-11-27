import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { keys } from '../../../shared/api/queryKeys';
import { QueryState } from '../../../shared/components/Page';
import { bitacoraApi } from '../services/bitacoraApi';
import { PAGE_SIZE } from '../types/bitacora';
import { ChangeLogList } from './ChangeLogList';

/** Bitácora tab of the CRM ficha: changes to the invitado and their addresses. */
export function InvitadoBitacora({ invitadoId }: { invitadoId: number }) {
  const [page, setPage] = useState(1);
  const log = useQuery({
    // Under the CRM key: saving anything in the ficha refreshes it.
    queryKey: [...keys.crm, 'bitacora', invitadoId, page],
    queryFn: () => bitacoraApi.invitado(invitadoId, { pagina: page, porPagina: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  });
  return (
    <section className="stack">
      <p className="muted">Quién cambió qué en los datos del invitado y sus direcciones, lo más nuevo primero.</p>
      <QueryState loading={log.isPending} error={log.error}>
        {log.data && <ChangeLogList data={log.data} setPage={setPage} />}
      </QueryState>
    </section>
  );
}

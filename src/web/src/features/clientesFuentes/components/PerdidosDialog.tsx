import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Dialog } from '../../../shared/components/Dialog';
import { QueryState } from '../../../shared/components/Page';
import { useSave } from '../../../shared/hooks/useSave';
import { shortDay } from '../../../shared/utils/dates';
import { clientesFuentesApi, clientesFuentesKey } from '../services/clientesFuentesApi';
import { money, type Period, type Tipo } from '../types/clientesFuentes';

/**
 * The invitados who became perdidos in the period (GET /gerencia/clientes/perdidos). With permission to edit, the
 * chosen ones go to "Llamar hoy" of Clientes (CRM) until someone registers a contact with them.
 */
export function PerdidosDialog({
  periodo,
  fecha,
  tipo,
  fuente,
  canEdit,
  onClose,
}: {
  periodo: Period;
  fecha: string;
  tipo: Tipo;
  fuente?: string;
  canEdit: boolean;
  onClose: () => void;
}) {
  const query = useQuery({
    queryKey: [...clientesFuentesKey, 'perdidos', periodo, fecha, tipo, fuente],
    queryFn: () => clientesFuentesApi.lost({ periodo, fecha, tipo, fuente }),
  });
  const rows = query.data?.rows;
  // Everyone who can be called and is not waiting for the call yet, chosen at the start.
  const callable = (r: NonNullable<typeof rows>[number]) => !r.doNotContact && !r.reactivation && !r.cameBack;
  const [chosen, setChosen] = useState<Set<number>>(new Set());
  useEffect(() => setChosen(new Set(rows?.filter(callable).map((r) => r.invitadoId))), [rows]);
  const [result, setResult] = useState<string | null>(null);
  const send = useSave(() => clientesFuentesApi.call({ invitadoIds: [...chosen] }), {
    refetch: [clientesFuentesKey],
    onDone: (r) => setResult(r.message),
  });

  const toggle = (id: number) =>
    setChosen((s) => {
      const next = new Set(s);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const title = `Perdidos${fuente ? ` · ${fuente}` : ''}${query.data ? ` · ${query.data.label}` : ''}`;
  const canSend = canEdit && chosen.size > 0 && !result;
  return (
    <Dialog
      title={title}
      onClose={onClose}
      onSubmit={() => (canSend ? send.mutate(undefined) : onClose())}
      busy={send.isPending}
      submitText={canSend ? `Mandar ${chosen.size} a "Llamar hoy"` : 'Cerrar'}
      hideCancel={!canSend}
      error={send.error}
      className="cyf-dialog"
    >
      {result && <div className="alert success">{result}</div>}
      <QueryState loading={query.isPending} error={query.error}>
        {rows?.length === 0 ? (
          <div className="empty">Nadie se volvió perdido en este periodo.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  {canEdit && <th></th>}
                  <th>Invitado</th>
                  <th>Fuente</th>
                  <th>Último pedido</th>
                  <th>Perdido desde</th>
                  <th className="end">Pedidos</th>
                  <th className="end">Venta</th>
                </tr>
              </thead>
              <tbody>
                {rows?.map((r) => (
                  <tr key={r.invitadoId}>
                    {canEdit && (
                      <td>
                        <input
                          type="checkbox"
                          checked={chosen.has(r.invitadoId)}
                          disabled={!callable(r) || !!result}
                          onChange={() => toggle(r.invitadoId)}
                          aria-label={`Llamar a ${r.name}`}
                        />
                      </td>
                    )}
                    <td>
                      <strong>{r.name}</strong>
                      <div className="muted">{r.whatsAppPhone}</div>
                      {r.doNotContact && <span className="badge danger">No contactar</span>}
                      {r.reactivation && <span className="badge brand">Ya está en "Llamar hoy"</span>}
                      {r.cameBack && <span className="badge success">Ya volvió</span>}
                    </td>
                    <td>{r.fuente}</td>
                    <td>{shortDay(r.lastService)}</td>
                    <td>{shortDay(r.lostOn)}</td>
                    <td className="end">{r.services}</td>
                    <td className="end">{money(r.venta)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </QueryState>
    </Dialog>
  );
}

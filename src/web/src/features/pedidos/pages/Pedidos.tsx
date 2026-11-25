import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { useRutas } from '../../operacion';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { Checkbox } from '../../../shared/components/Form';
import { PedidoCard } from '../components/PedidoCard';
import { PedidoDialog } from '../components/PedidoDialog';
import { pedidosApi } from '../services/pedidosApi';

/**
 * Pedidos (docs/pantallas/oficina/pedidos.md): the way of each pedido from recolección to entrega. Etapas move
 * with the chofer and planta; moving a card by hand is for exceptions.
 */
export function Pedidos() {
  const rutas = useRutas();
  const [rutaId, setRutaId] = useState<number | null>(null);
  const [text, setText] = useState('');
  const [q, setQ] = useState('');
  const [onlyLate, setOnlyLate] = useState(false);
  // ?pedido=ID opens its ficha (from the office bell or WhatsApp y metas).
  const [params, setParams] = useSearchParams();
  const linked = Number(params.get('pedido')) || null;
  const [open, setOpen] = useState<number | null>(linked);
  const [lastLinked, setLastLinked] = useState(linked);
  if (linked !== lastLinked) {
    setLastLinked(linked);
    if (linked) setOpen(linked);
  }
  function close() {
    setOpen(null);
    if (params.has('pedido')) setParams({}, { replace: true });
  }

  useEffect(() => {
    const t = setTimeout(() => setQ(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);

  const board = useQuery({
    queryKey: [...keys.pedidos, 'tablero', rutaId, q, onlyLate],
    queryFn: () => pedidosApi.board({ ruta: rutaId ?? undefined, q: q || undefined, atrasados: onlyLate }),
    placeholderData: (previous) => previous,
    refetchInterval: 60_000,
  });
  const b = board.data;

  return (
    <div className="stack">
      <PageHeader
        title="Pedidos"
        description="El camino de cada pedido, de la recolección a la entrega. Las etapas avanzan solas con el chofer y planta."
      >
        <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Invitado, folio o teléfono" style={{ maxWidth: 230 }} />
        <select value={rutaId ?? ''} onChange={(e) => setRutaId(e.target.value ? Number(e.target.value) : null)} style={{ width: 'auto' }}>
          <option value="">Todas las rutas</option>
          {rutas.data
            ?.filter((r) => r.active)
            .map((r) => (
              <option key={r.id} value={r.id}>
                Ruta {r.code}
              </option>
            ))}
        </select>
        <Checkbox checked={onlyLate} onChange={setOnlyLate}>
          Atrasados e intentos fallidos{b ? ` (${b.lateCount})` : ''}
        </Checkbox>
      </PageHeader>
      <QueryState loading={board.isPending} error={board.error}>
        <div className="pedidos-board">
          {b?.columns.map((c) => (
            <section key={c.etapa} className="pedidos-column">
              <header>
                <strong>{c.name}</strong>
                <span className="badge">{c.count}</span>
              </header>
              {c.etapa === 'Entregado' && <p className="muted pedidos-hint">Últimos 7 días</p>}
              <div className="pedidos-cards">
                {c.cards.map((card) => (
                  <PedidoCard key={card.id} card={card} onOpen={() => setOpen(card.id)} />
                ))}
                {c.cards.length === 0 && <p className="muted pedidos-hint">Sin pedidos</p>}
              </div>
            </section>
          ))}
        </div>
      </QueryState>
      {open && <PedidoDialog id={open} onClose={close} />}
    </div>
  );
}

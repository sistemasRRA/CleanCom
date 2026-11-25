import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import type { Card, Ticket, View } from '../types/planta';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { Stat } from '../components/Stat';
import { ScanBox } from '../components/ScanBox';
import { PlantaCard } from '../components/PlantaCard';
import { CaptureDialog } from '../components/CaptureDialog';
import { CommentDialog } from '../components/CommentDialog';
import { BajaDialog } from '../components/BajaDialog';
import { plantaApi } from '../services/plantaApi';

/**
 * Tickets en planta (docs/pantallas/oficina/tickets-en-planta.md): planta scans or types each folio when it is ready.
 * The last ticket of a pedido moves it to Listo. Until the POS connection is defined, tickets are captured here.
 */
export function TicketsEnPlanta() {
  const canEdit = useSession().can('tickets-en-planta', 'edit');
  const [view, setView] = useState<View>('EnProceso');
  const [capture, setCapture] = useState(false);
  const [comment, setComment] = useState<Card | null>(null);
  const [baja, setBaja] = useState<Ticket | null>(null);

  const board = useQuery({
    queryKey: [...keys.planta, view],
    queryFn: () => plantaApi.tickets({ vista: view }),
    placeholderData: (previous) => previous,
    refetchInterval: 60_000,
  });
  const b = board.data;
  const i = b?.indicators;

  return (
    <div className="stack">
      <PageHeader
        title="Tickets en planta"
        description="Marca cada ticket cuando quede listo. Cuando todos los tickets de un pedido están listos, el pedido pasa a «Listo» y Logística confirma la entrega con el invitado. Si una prenda se atrasa, deja un comentario: Logística lo ve en el tablero de Pedidos."
      >
        {canEdit && (
          <button type="button" onClick={() => setCapture(true)}>
            Capturar ticket
          </button>
        )}
      </PageHeader>

      {i && (
        <div className="planta-stats">
          <Stat value={i.ticketsInProcess} label="Tickets en proceso" tone="brand" />
          <Stat value={i.pedidosReady} label="Pedidos 100% listos" tone="success" />
          <Stat value={i.pedidosPartial} label="Pedidos parciales" tone="warning" />
          <Stat
            value={i.entregaDueOrLate}
            label="Con entrega hoy o atrasada"
            tone="danger"
            note={`${i.manyDaysInPlanta} con ${i.thresholdDays} días o más en planta`}
          />
        </div>
      )}

      {canEdit && <ScanBox />}

      <div className="actions tight">
        <button type="button" className={`small${view === 'EnProceso' ? ' selected' : ''}`} onClick={() => setView('EnProceso')}>
          En proceso
        </button>
        <button type="button" className={`small${view === 'Listos' ? ' selected' : ''}`} onClick={() => setView('Listos')}>
          Listos sin entregar
        </button>
      </div>

      <QueryState loading={board.isPending} error={board.error}>
        <div className="planta-cards">
          {b?.cards.map((c) => (
            <PlantaCard key={c.pedido.id} card={c} canEdit={canEdit} onComment={() => setComment(c)} onBaja={setBaja} />
          ))}
          {b && b.cards.length === 0 && (
            <p className="empty">{view === 'EnProceso' ? 'No hay tickets en proceso.' : 'No hay pedidos listos sin entregar.'}</p>
          )}
        </div>
      </QueryState>

      {capture && <CaptureDialog onClose={() => setCapture(false)} />}
      {comment && <CommentDialog card={comment} onClose={() => setComment(null)} />}
      {baja && <BajaDialog ticket={baja} onClose={() => setBaja(null)} />}
    </div>
  );
}

import { useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys } from '../../api/queries';
import { useSession } from '../../session/Session';
import { Dialog, Field, PageHeader, QueryState, optional, useSave } from '../../components/Common';

type View = Schemas['PlantaView'];
type Card = Schemas['PlantaCardDto'];
type Ticket = Schemas['TicketDto'];

const shortDate = new Intl.DateTimeFormat('es-MX', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
const short = (iso?: string | null) => (iso ? shortDate.format(new Date(`${iso}T00:00:00Z`)).replace('.', '') : '—');
const refetch = [keys.planta, keys.pedidos];

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
    queryFn: () => unwrap(api.GET('/planta/tickets', { params: { query: { vista: view } } })),
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

function Stat({ value, label, tone, note }: { value: number; label: string; tone: 'brand' | 'success' | 'warning' | 'danger'; note?: string }) {
  return (
    <div className={`planta-stat ${tone}`}>
      <span className="planta-stat__label">{label}</span>
      <strong>{value}</strong>
      {note && <span className="planta-stat__note">{note}</span>}
    </div>
  );
}

/** Continuous capture: a barcode scanner types the folio and presses Enter; an error never blocks the next one. */
function ScanBox() {
  const queryClient = useQueryClient();
  const input = useRef<HTMLInputElement>(null);
  const [folio, setFolio] = useState('');
  const [result, setResult] = useState<{ ok: boolean; text: string } | null>(null);
  const ready = useMutation({
    mutationFn: (f: string) => unwrap(api.POST('/planta/tickets/{folio}/listo', { params: { path: { folio: f } } })),
    onSuccess: async (r) => {
      setResult({ ok: true, text: r.message });
      await Promise.all(refetch.map((k) => queryClient.invalidateQueries({ queryKey: k })));
    },
    onError: (e) => setResult({ ok: false, text: e.message }),
    onSettled: () => input.current?.focus(),
  });

  return (
    <form
      className="planta-scan"
      onSubmit={(e) => {
        e.preventDefault();
        const f = folio.trim();
        if (!f) return;
        setFolio('');
        ready.mutate(f);
      }}
    >
      <input
        ref={input}
        autoFocus
        value={folio}
        onChange={(e) => setFolio(e.target.value)}
        placeholder="Escanea o escribe el folio y presiona Enter"
        aria-label="Folio del ticket"
        autoComplete="off"
      />
      <button type="submit" className="primary" disabled={ready.isPending || !folio.trim()}>
        Marcar listo
      </button>
      {result && <div className={`alert ${result.ok ? 'success' : 'error'}`}>{result.text}</div>}
    </form>
  );
}

function PlantaCard({ card: c, canEdit, onComment, onBaja }: { card: Card; canEdit: boolean; onComment: () => void; onBaja: (t: Ticket) => void }) {
  const queryClient = useQueryClient();
  const ready = useMutation({
    mutationFn: (folio: string) => unwrap(api.POST('/planta/tickets/{folio}/listo', { params: { path: { folio } } })),
    onSuccess: () => Promise.all(refetch.map((k) => queryClient.invalidateQueries({ queryKey: k }))),
  });
  const p = c.pedido;
  const late = c.tickets.some((t) => t.delayLevel === 'Late');

  return (
    <article className={`planta-card${c.entregaDueOrLate || late ? ' late' : ''}`}>
      <header>
        {p.rutaCode && (
          <span>
            <span className="ruta-color" style={{ background: p.rutaColor ?? undefined }} />
            {p.rutaCode}
          </span>
        )}
        <span className="muted">#{p.folio}</span>
        <span className={`badge ${c.ticketsReady === c.ticketsTotal ? 'success' : ''}`}>
          {c.ticketsReady}/{c.ticketsTotal} listos
        </span>
      </header>
      <strong>{p.invitadoName}</strong>
      <span className="muted">
        {c.daysInPlanta} día{c.daysInPlanta === 1 ? '' : 's'} en planta
        {p.entregaDate && ` · entrega ${short(p.entregaDate)}`}
      </span>
      {c.entregaDueOrLate && <span className="badge danger">Entrega hoy o atrasada</span>}
      <ul className="planta-tickets">
        {c.tickets.map((t) => (
          <li key={t.id} className={t.state === 'Listo' ? 'done' : ''}>
            <div>
              <strong>{t.folio}</strong>
              <span className="muted">
                {t.lineaName} · {t.garments} prenda{t.garments === 1 ? '' : 's'}
              </span>
              {t.delayText && <span className={`badge ${t.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{t.delayText}</span>}
            </div>
            {t.state === 'Listo' ? (
              <span className="badge success">Listo</span>
            ) : (
              canEdit && (
                <span className="actions tight">
                  <button type="button" className="small" title="Dar de baja (capturado por error)" onClick={() => onBaja(t)}>
                    Baja
                  </button>
                  <button type="button" className="primary" disabled={ready.isPending} onClick={() => ready.mutate(t.folio)}>
                    Listo
                  </button>
                </span>
              )
            )}
          </li>
        ))}
      </ul>
      {ready.error && <div className="alert error">{ready.error.message}</div>}
      {c.lastDelayNote && <p className="muted">Atraso: {c.lastDelayNote}</p>}
      {canEdit && c.ticketsReady < c.ticketsTotal && (
        <button type="button" className="small link" onClick={onComment}>
          ¿Por qué se atrasa? Comentar
        </button>
      )}
    </article>
  );
}

function CaptureDialog({ onClose }: { onClose: () => void }) {
  const lineas = useQuery({ queryKey: keys.lineasServicio, queryFn: () => unwrap(api.GET('/lineas-servicio')) });
  const [pedidoId, setPedidoId] = useState('');
  const [folio, setFolio] = useState('');
  const [lineaId, setLineaId] = useState('');
  const [garments, setGarments] = useState('1');
  const [amount, setAmount] = useState('');
  const save = useSave(
    () =>
      unwrap(
        api.POST('/planta/tickets', {
          body: {
            folio: folio.trim(),
            pedidoId: Number(pedidoId),
            lineaServicioId: Number(lineaId),
            garments: Number(garments),
            amount: amount.trim() ? Number(amount) : null,
          },
        }),
      ),
    {
      refetch,
      message: 'Ticket capturado.',
      onDone: () => {
        // Keeps the pedido and línea for the next ticket of the same pedido.
        setFolio('');
        setGarments('1');
        setAmount('');
      },
    },
  );

  return (
    <Dialog title="Capturar ticket" onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} submitText="Capturar" error={save.error}>
      <p className="muted">Mientras se conecta el POS, los tickets se capturan aquí. El primer ticket pasa el pedido a En planta.</p>
      <div className="form-grid">
        <Field label="Pedido (folio)">
          <input inputMode="numeric" value={pedidoId} onChange={(e) => setPedidoId(e.target.value.replace(/\D/g, ''))} autoFocus />
        </Field>
        <Field label="Folio del ticket">
          <input value={folio} onChange={(e) => setFolio(e.target.value)} autoComplete="off" />
        </Field>
        <Field label="Línea de servicio">
          <select value={lineaId} onChange={(e) => setLineaId(e.target.value)}>
            <option value="">Elige la línea</option>
            {lineas.data
              ?.filter((l) => l.active)
              .map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                </option>
              ))}
          </select>
        </Field>
        <Field label="Prendas">
          <input type="number" min={1} value={garments} onChange={(e) => setGarments(e.target.value)} />
        </Field>
        <Field label="Monto (opcional)">
          <input type="number" min={0} step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
        </Field>
      </div>
    </Dialog>
  );
}

function CommentDialog({ card, onClose }: { card: Card; onClose: () => void }) {
  const reasons = useQuery({ queryKey: keys.reasons, queryFn: () => unwrap(api.GET('/motivos')) });
  const delay = reasons.data?.filter((r) => r.type === 'PlantaDelay' && r.active) ?? [];
  const [reasonId, setReasonId] = useState('');
  const [note, setNote] = useState('');
  const save = useSave(
    () => unwrap(api.POST('/planta/comentarios', { body: { pedidoId: card.pedido.id, reasonId: reasonId ? Number(reasonId) : null, note: optional(note) } })),
    { refetch, message: 'Comentario guardado; se ve en Pedidos.', onDone: onClose },
  );
  return (
    <Dialog
      title={`¿Por qué se atrasa? · Pedido #${card.pedido.folio}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        {delay.length > 0 && (
          <Field label="Motivo">
            <select value={reasonId} onChange={(e) => setReasonId(e.target.value)}>
              <option value="">Elige el motivo</option>
              {delay.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.text}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label={delay.length > 0 ? 'Detalle' : 'Comentario'} wide={delay.length === 0}>
          <input value={note} onChange={(e) => setNote(e.target.value)} placeholder="Falta un botón en el saco" />
        </Field>
      </div>
    </Dialog>
  );
}

function BajaDialog({ ticket, onClose }: { ticket: Ticket; onClose: () => void }) {
  const [reason, setReason] = useState('');
  const save = useSave(() => unwrap(api.POST('/planta/tickets/{id}/baja', { params: { path: { id: ticket.id } }, body: { reason: reason.trim() } })), {
    refetch,
    message: `Ticket ${ticket.folio} dado de baja.`,
    onDone: onClose,
  });
  return (
    <Dialog title={`Dar de baja el ticket ${ticket.folio}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} submitText="Dar de baja" error={save.error}>
      <p className="muted">Solo para tickets capturados por error. Queda en el historial del pedido.</p>
      <Field label="Motivo" wide>
        <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Era de otro pedido" autoFocus />
      </Field>
    </Dialog>
  );
}

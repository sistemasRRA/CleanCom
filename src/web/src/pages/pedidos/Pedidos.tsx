import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useRutas } from '../../api/queries';
import { useSession } from '../../session/Session';
import { Checkbox, Dialog, Field, PageHeader, QueryState, formatDateTime, optional, useSave } from '../../components/Common';

type Card = Schemas['PedidoCardDto'];
type Etapa = Schemas['PedidoEtapa'];
type Detail = Schemas['PedidoDetailDto'];

const ETAPAS: { key: Etapa; name: string }[] = [
  { key: 'Agendado', name: 'Agendado' },
  { key: 'Recolectado', name: 'Recolectado' },
  { key: 'EnPlanta', name: 'En planta' },
  { key: 'Listo', name: 'Listo' },
  { key: 'EntregaConfirmada', name: 'Entrega confirmada' },
  { key: 'EnCamino', name: 'En camino' },
  { key: 'Entregado', name: 'Entregado' },
  { key: 'Cancelado', name: 'Cancelado' },
];
const etapaName = (e: Etapa) => ETAPAS.find((x) => x.key === e)?.name ?? e;
const KIND: Record<string, string> = { Recoleccion: 'Recolección', Entrega: 'Entrega' };
const STATE: Record<Schemas['ParadaState'], string> = {
  Pendiente: 'Pendiente',
  EnCamino: 'En camino',
  EnSitio: 'Llegó',
  Realizada: 'Realizada',
  NoRealizada: 'No realizada',
};

const shortDate = new Intl.DateTimeFormat('es-MX', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
const short = (iso?: string | null) => (iso ? shortDate.format(new Date(`${iso}T00:00:00Z`)).replace('.', '') : '—');

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
  const [open, setOpen] = useState<number | null>(null);

  useEffect(() => {
    const t = setTimeout(() => setQ(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);

  const board = useQuery({
    queryKey: [...keys.pedidos, 'tablero', rutaId, q, onlyLate],
    queryFn: () =>
      unwrap(api.GET('/pedidos/tablero', { params: { query: { ruta: rutaId ?? undefined, q: q || undefined, atrasados: onlyLate } } })),
    placeholderData: (previous) => previous,
    refetchInterval: 60_000,
  });
  const b = board.data;

  return (
    <div className="stack">
      <PageHeader title="Pedidos" description="El camino de cada pedido, de la recolección a la entrega. Las etapas avanzan solas con el chofer y planta.">
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
      {open && <PedidoDialog id={open} onClose={() => setOpen(null)} />}
    </div>
  );
}

function PedidoCard({ card: c, onOpen }: { card: Card; onOpen: () => void }) {
  const entrega = ['EntregaConfirmada', 'EnCamino', 'Entregado'].includes(c.etapa);
  return (
    <button type="button" className={`pedido-card ${c.delayLevel === 'Late' ? 'late' : c.delayLevel === 'Warning' ? 'warn' : ''}`} onClick={onOpen}>
      <div className="pedido-card__top">
        {c.rutaCode && (
          <span>
            <span className="ruta-color" style={{ background: c.rutaColor ?? undefined }} />
            {c.rutaCode}
          </span>
        )}
        <span className="muted">#{c.folio}</span>
        {c.priority && <span className="badge danger">Prioridad</span>}
      </div>
      <strong>{c.invitadoName}</strong>
      <span className="muted">
        {entrega ? `Entrega ${short(c.entregaDate)}` : `Recolección ${short(c.recoleccionDate)}`}
        {c.garmentsApprox != null && ` · ${c.garmentsApprox} prendas`}
      </span>
      {c.daysInStore != null && <span className="muted">{c.daysInStore} días en tienda</span>}
      {c.ticketsTotal > 0 && (
        <span className={`badge ${c.ticketsReady === c.ticketsTotal ? 'success' : ''}`}>
          {c.ticketsReady}/{c.ticketsTotal} tickets listos
        </span>
      )}
      {c.delayText && <span className={`badge ${c.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{c.delayText}</span>}
      {c.failedAttempts > 0 && c.etapa !== 'Entregado' && (
        <span className="badge danger">
          {c.failedAttempts} intento{c.failedAttempts > 1 ? 's' : ''} fallido{c.failedAttempts > 1 ? 's' : ''}
        </span>
      )}
    </button>
  );
}

type Action = 'mover' | 'entrega' | 'no-entregado' | 'comentar' | null;

function PedidoDialog({ id, onClose }: { id: number; onClose: () => void }) {
  const canEdit = useSession().can('pedidos', 'edit');
  const detail = useQuery({ queryKey: [...keys.pedidos, 'detalle', id], queryFn: () => unwrap(api.GET('/pedidos/{id}', { params: { path: { id } } })) });
  const [action, setAction] = useState<Action>(null);
  const d = detail.data;

  return (
    <Dialog title={d ? `Pedido #${d.card.folio} · ${d.card.invitadoName}` : 'Pedido'} onClose={onClose} onSubmit={onClose} submitText="Cerrar">
      <QueryState loading={detail.isPending} error={detail.error}>
        {d && (
          <div className="stack">
            <div className="pedido-summary">
              <span className="badge brand">{etapaName(d.card.etapa)}</span>
              {d.card.delayText && <span className={`badge ${d.card.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{d.card.delayText}</span>}
              {d.card.failedAttempts > 0 && <span className="badge danger">{d.card.failedAttempts} intento(s) fallido(s)</span>}
            </div>
            <dl className="pedido-facts">
              <dt>Dirección</dt>
              <dd>
                {d.address}
                {d.card.zonaCode && ` · ${d.card.zonaCode}`}
                {d.card.rutaCode && ` · Ruta ${d.card.rutaCode}`}
              </dd>
              <dt>Recolección</dt>
              <dd>{short(d.card.recoleccionDate)}</dd>
              <dt>Entrega</dt>
              <dd>{short(d.card.entregaDate)}</dd>
              <dt>Prendas · líneas</dt>
              <dd>
                {d.card.garmentsApprox ?? '—'} · {d.card.lineas.join(', ') || '—'}
              </dd>
              {d.driverNotes && (
                <>
                  <dt>Notas</dt>
                  <dd>{d.driverNotes}</dd>
                </>
              )}
              <dt>Invitado</dt>
              <dd>
                {d.card.phone}{' '}
                <Link to={`/clientes?invitado=${d.card.invitadoId}`} onClick={onClose}>
                  Ver en CRM
                </Link>
              </dd>
            </dl>

            {canEdit && d.card.etapa !== 'Cancelado' && (
              <div className="actions tight">
                <button type="button" className={`small${action === 'mover' ? ' selected' : ''}`} onClick={() => setAction('mover')}>
                  Cambiar etapa
                </button>
                {['EnPlanta', 'Listo', 'EntregaConfirmada'].includes(d.card.etapa) && (
                  <button type="button" className={`small${action === 'entrega' ? ' selected' : ''}`} onClick={() => setAction('entrega')}>
                    {d.card.etapa === 'EntregaConfirmada' ? 'Cambiar fecha de entrega' : 'Programar entrega'}
                  </button>
                )}
                {['EntregaConfirmada', 'EnCamino'].includes(d.card.etapa) && (
                  <button type="button" className={`small${action === 'no-entregado' ? ' selected' : ''}`} onClick={() => setAction('no-entregado')}>
                    No se entregó
                  </button>
                )}
                <button type="button" className={`small${action === 'comentar' ? ' selected' : ''}`} onClick={() => setAction('comentar')}>
                  Comentar
                </button>
              </div>
            )}
            {action === 'mover' && <MoveForm detail={d} onDone={() => setAction(null)} />}
            {action === 'entrega' && <EntregaForm id={id} onDone={() => setAction(null)} />}
            {action === 'no-entregado' && <NoEntregadoForm id={id} onDone={() => setAction(null)} />}
            {action === 'comentar' && <CommentForm id={id} onDone={() => setAction(null)} />}

            {d.tickets.length > 0 && (
              <>
                <h3 className="form-section">Tickets</h3>
                <ul className="plain-list">
                  {d.tickets.map((t) => (
                    <li key={t.id}>
                      <strong>{t.folio}</strong> · {t.lineaName} · {t.garments} prenda{t.garments === 1 ? '' : 's'} ·{' '}
                      {t.state === 'Listo' ? `listo ${formatDateTime(t.readyAt)}` : 'en proceso'}
                      {t.delayText && <span className={`badge ${t.delayLevel === 'Late' ? 'danger' : 'warning'}`}>{t.delayText}</span>}
                    </li>
                  ))}
                </ul>
              </>
            )}
            <h3 className="form-section">Visitas</h3>
            <ul className="plain-list">
              {d.paradas.map((p) => (
                <li key={p.id} className={p.active ? '' : 'muted'}>
                  <strong>{KIND[p.kind] ?? p.kind}</strong> {p.label} · Ruta {p.rutaCode} · {p.active ? STATE[p.state] : 'quitada'}
                  {p.arrivedAt && ` · llegó ${formatDateTime(p.arrivedAt)}`}
                </li>
              ))}
            </ul>
            <h3 className="form-section">Historial</h3>
            <ul className="plain-list pedido-history">
              {d.history.map((h) => (
                <li key={h.id}>
                  <span className="muted">{formatDateTime(h.at)}</span> {h.text}
                  {h.by && <span className="muted"> — {h.by}</span>}
                </li>
              ))}
            </ul>
          </div>
        )}
      </QueryState>
    </Dialog>
  );
}

const refetch = [keys.pedidos, keys.agendaBoard, keys.rutasDia];

function MoveForm({ detail: d, onDone }: { detail: Detail; onDone: () => void }) {
  const [etapa, setEtapa] = useState<Etapa>(d.card.etapa);
  const [reason, setReason] = useState('');
  const save = useSave(
    () => unwrap(api.POST('/pedidos/{id}/etapa', { params: { path: { id: d.card.id } }, body: { etapa, reason: optional(reason) } })),
    { refetch, message: 'Etapa cambiada.', onDone },
  );
  return (
    <div className="inline-form">
      <p className="muted">Las etapas avanzan solas; mover a mano es para excepciones. Regresar, entregar o cancelar a mano pide motivo.</p>
      <div className="form-grid">
        <Field label="Nueva etapa">
          <select value={etapa} onChange={(e) => setEtapa(e.target.value as Etapa)}>
            {ETAPAS.map((e) => (
              <option key={e.key} value={e.key}>
                {e.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Motivo">
          <input value={reason} onChange={(e) => setReason(e.target.value)} />
        </Field>
      </div>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending || etapa === d.card.etapa} onClick={() => save.mutate(undefined)}>
        Guardar etapa
      </button>
    </div>
  );
}

function EntregaForm({ id, onDone }: { id: number; onDone: () => void }) {
  const days = useQuery({ queryKey: [...keys.pedidos, 'fechas-entrega', id], queryFn: () => unwrap(api.GET('/pedidos/{id}/fechas-entrega', { params: { path: { id } } })) });
  const [date, setDate] = useState<string | null>(null);
  const save = useSave(
    () => unwrap(api.POST('/pedidos/{id}/programar-entrega', { params: { path: { id } }, body: { date } })),
    { refetch, onDone: (r) => (r.scheduled ? onDone() : undefined) },
  );
  return (
    <div className="inline-form">
      <QueryState loading={days.isPending} error={days.error}>
        {days.data && (
          <>
            <p className="muted">
              Zona {days.data.zonaCode} · Ruta {days.data.rutaCode}. Elige el día con cupo; la entrega ocupa un lugar como una recolección.
            </p>
            <div className="entrega-days">
              {days.data.days.map((d) => (
                <button
                  key={d.date}
                  type="button"
                  className={`small${date === d.date ? ' selected' : ''}`}
                  disabled={!d.available}
                  onClick={() => setDate(d.date)}
                >
                  {d.label} · {d.occupied}/{d.cupo}
                </button>
              ))}
            </div>
          </>
        )}
      </QueryState>
      {save.data && !save.data.scheduled && <div className="alert">{save.data.message}</div>}
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending || !date} onClick={() => save.mutate(undefined)}>
        Programar entrega
      </button>
    </div>
  );
}

function NoEntregadoForm({ id, onDone }: { id: number; onDone: () => void }) {
  const reasons = useQuery({ queryKey: keys.reasons, queryFn: () => unwrap(api.GET('/motivos')) });
  const failed = reasons.data?.filter((r) => r.type === 'FailedEntrega' && r.active) ?? [];
  const [reasonId, setReasonId] = useState('');
  const [note, setNote] = useState('');
  const save = useSave(
    () =>
      unwrap(api.POST('/pedidos/{id}/no-entregado', { params: { path: { id } }, body: { reasonId: reasonId ? Number(reasonId) : null, note: optional(note) } })),
    { refetch, message: 'Intento registrado y entrega reprogramada.', onDone },
  );
  return (
    <div className="inline-form">
      <p className="muted">Queda en el historial y se reprograma al siguiente día con cupo de su zona, desde mañana.</p>
      <div className="form-grid">
        {failed.length > 0 && (
          <Field label="Motivo">
            <select value={reasonId} onChange={(e) => setReasonId(e.target.value)}>
              <option value="">Elige el motivo</option>
              {failed.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.text}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label={failed.length > 0 ? 'Detalle' : 'Motivo'} wide={failed.length === 0}>
          <input value={note} onChange={(e) => setNote(e.target.value)} placeholder="No había nadie" />
        </Field>
      </div>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending} onClick={() => save.mutate(undefined)}>
        Registrar y reprogramar
      </button>
    </div>
  );
}

function CommentForm({ id, onDone }: { id: number; onDone: () => void }) {
  const [note, setNote] = useState('');
  const save = useSave(() => unwrap(api.POST('/pedidos/{id}/comentarios', { params: { path: { id } }, body: { note: note.trim() } })), {
    refetch,
    message: 'Comentario guardado.',
    onDone,
  });
  return (
    <div className="inline-form">
      <Field label="Comentario" wide>
        <input value={note} onChange={(e) => setNote(e.target.value)} />
      </Field>
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending || !note.trim()} onClick={() => save.mutate(undefined)}>
        Guardar comentario
      </button>
    </div>
  );
}

import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys } from '../../api/queries';
import { useSession } from '../../session/Session';
import { QueryState, formatDate, useAction } from '../../components/Common';

type Pedido = Schemas['PedidoDto'];
type Board = Schemas['AgendaBoardDto'];
type Ruta = Schemas['BoardRutaDto'];
type Turno = Schemas['Turno'];
type View = 'semana' | 'dia';

/** Rows of the day view, in the order the office reads them. */
const TURNOS: { key: Turno; name: string; hint: string }[] = [
  { key: 'Manana', name: 'Mañana', hint: '9:00 a 13:00' },
  { key: 'Tarde', name: 'Tarde', hint: '13:00 a 18:00' },
  { key: 'HoraEspecifica', name: 'Hora específica', hint: 'Solo oficina' },
  { key: 'CualquierHora', name: 'Cualquier hora', hint: 'Toda la jornada' },
];
const TURNO_ORDER: Record<Turno, number> = { Manana: 0, Tarde: 1, HoraEspecifica: 2, CualquierHora: 3 };

// ---- Business dates (yyyy-MM-dd) without time zone shifts ----
const toDate = (iso: string) => new Date(`${iso}T00:00:00Z`);
const toIso = (d: Date) => d.toISOString().slice(0, 10);
const addDays = (iso: string, n: number) => {
  const d = toDate(iso);
  d.setUTCDate(d.getUTCDate() + n);
  return toIso(d);
};
const mondayOf = (iso: string) => addDays(iso, -((toDate(iso).getUTCDay() + 6) % 7));
const monthTitle = new Intl.DateTimeFormat('es-MX', { month: 'long', year: 'numeric', timeZone: 'UTC' });
const weekday = new Intl.DateTimeFormat('es-MX', { weekday: 'short', timeZone: 'UTC' });
/** Today in Mexico City, before the API answers. */
const todayInMexicoCity = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(new Date());

function sortPedidos(list: Pedido[]) {
  return [...list].sort(
    (a, b) =>
      (a.rutaCode ?? '').localeCompare(b.rutaCode ?? '') ||
      TURNO_ORDER[a.turno] - TURNO_ORDER[b.turno] ||
      (a.specificTime ?? '').localeCompare(b.specificTime ?? '') ||
      a.invitadoName.localeCompare(b.invitadoName),
  );
}

/**
 * Agenda of recolecciones: week (7 days with the occupancy of each ruta) or day (one column per ruta, rows by turno).
 * Occupancy and cupo come computed from the API (GET /agenda/tablero); the screen only draws them.
 */
export function AgendaBoard() {
  const [view, setView] = useState<View>('semana');
  const [date, setDate] = useState(todayInMexicoCity);
  const [detail, setDetail] = useState<Pedido | null>(null);

  const from = view === 'semana' ? mondayOf(date) : date;
  const days = view === 'semana' ? 7 : 1;
  const board = useQuery({
    queryKey: [...keys.agendaBoard, from, days],
    queryFn: () => unwrap(api.GET('/agenda/tablero', { params: { query: { from, days } } })),
    placeholderData: (previous) => previous,
  });
  const b = board.data;
  const today = b?.today ?? todayInMexicoCity();

  const step = view === 'semana' ? 7 : 1;
  const title = view === 'semana' ? monthTitle.format(toDate(from)) : formatDate(date);

  return (
    <section className="card">
      <div className="card__header agenda__toolbar">
        <div className="actions">
          <h2>Agenda</h2>
          <button type="button" className="small" onClick={() => setDate(addDays(date, -step))} aria-label="Anterior">
            ‹
          </button>
          <button type="button" className="small" onClick={() => setDate(today)}>
            Hoy
          </button>
          <button type="button" className="small" onClick={() => setDate(addDays(date, step))} aria-label="Siguiente">
            ›
          </button>
          <strong className="agenda__title">{title}</strong>
        </div>
        <div className="actions">
          <input type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} aria-label="Ir a la fecha" />
          <div className="segmented">
            {(['semana', 'dia'] as View[]).map((v) => (
              <button key={v} type="button" className={`segmented__option${view === v ? ' selected' : ''}`} onClick={() => setView(v)}>
                {v === 'semana' ? 'Semana' : 'Día'}
              </button>
            ))}
          </div>
        </div>
      </div>
      <QueryState loading={board.isPending} error={board.error}>
        {b &&
          (view === 'semana' ? (
            <WeekView
              board={b}
              today={today}
              selected={date}
              onDay={(d) => {
                setDate(d);
                setView('dia');
              }}
              onPedido={setDetail}
            />
          ) : (
            <DayView board={b} onPedido={setDetail} />
          ))}
      </QueryState>
      {detail && <PedidoDetail pedido={detail} ruta={b?.rutas.find((r) => r.code === detail.rutaCode)} onClose={() => setDetail(null)} />}
    </section>
  );
}

function Occupancy({ ruta, occupied, cupo }: { ruta: Ruta; occupied: number; cupo: number }) {
  const full = occupied >= cupo;
  return (
    <div className={`occupancy${full ? ' full' : ''}`} title={`Ruta ${ruta.code}: ${occupied} de ${cupo} paradas`}>
      <span className="occupancy__label">
        <span className="ruta-color" style={{ background: ruta.color }} />
        {ruta.code} {occupied}/{cupo}
        {full && <strong> lleno</strong>}
      </span>
      <span className="occupancy__bar">
        <span style={{ width: `${Math.min(100, (occupied / Math.max(cupo, 1)) * 100)}%`, background: full ? undefined : ruta.color }} />
      </span>
    </div>
  );
}

function WeekView({
  board,
  today,
  selected,
  onDay,
  onPedido,
}: {
  board: Board;
  today: string;
  selected: string;
  onDay: (date: string) => void;
  onPedido: (p: Pedido) => void;
}) {
  return (
    <div className="agenda-week">
      {board.days.map((d) => {
        const list = sortPedidos(board.pedidos.filter((p) => p.recoleccionDate === d.date));
        return (
          <div key={d.date} className={`agenda-day${d.date === today ? ' today' : ''}${d.date === selected ? ' selected' : ''}${d.nonWorking ? ' off' : ''}`}>
            <button type="button" className="agenda-day__header" onClick={() => onDay(d.date)} title="Ver el día">
              <span className="muted">{weekday.format(toDate(d.date))}</span>
              <span className="agenda-day__number">{toDate(d.date).getUTCDate()}</span>
            </button>
            {d.nonWorking ? (
              <div className="badge warning">Inhábil: {d.nonWorking}</div>
            ) : (
              <div className="agenda-day__occupancy">
                {board.rutas.map((r) => {
                  const o = d.rutas.find((x) => x.rutaId === r.id);
                  return o && (o.occupied > 0 || d.date >= today) ? <Occupancy key={r.id} ruta={r} occupied={o.occupied} cupo={o.cupo} /> : null;
                })}
              </div>
            )}
            <div className="agenda-day__list">
              {list.map((p) => (
                <PedidoCard key={p.id} pedido={p} ruta={board.rutas.find((r) => r.code === p.rutaCode)} onClick={() => onPedido(p)} compact />
              ))}
              {list.length === 0 && !d.nonWorking && <span className="muted agenda-day__empty">Sin recolecciones</span>}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function DayView({ board, onPedido }: { board: Board; onPedido: (p: Pedido) => void }) {
  const day = board.days[0];
  if (!day) return null;
  if (board.rutas.length === 0) return <div className="empty">No hay rutas activas.</div>;
  return (
    <div className="agenda-dayview" style={{ gridTemplateColumns: `140px repeat(${board.rutas.length}, minmax(220px, 1fr))` }}>
      <div />
      {board.rutas.map((r) => {
        const o = day.rutas.find((x) => x.rutaId === r.id);
        return (
          <div key={r.id} className="agenda-dayview__ruta">
            <strong>{r.name}</strong>
            {o && <Occupancy ruta={r} occupied={o.occupied} cupo={o.cupo} />}
          </div>
        );
      })}
      {day.nonWorking && <div className="alert agenda-dayview__off">Día inhábil: {day.nonWorking}</div>}
      {TURNOS.map((t) => (
        <DayRow key={t.key} turno={t} board={board} onPedido={onPedido} />
      ))}
    </div>
  );
}

function DayRow({ turno, board, onPedido }: { turno: (typeof TURNOS)[number]; board: Board; onPedido: (p: Pedido) => void }) {
  return (
    <>
      <div className="agenda-dayview__turno">
        <strong>{turno.name}</strong>
        <span className="muted">{turno.hint}</span>
      </div>
      {board.rutas.map((r) => {
        const list = sortPedidos(board.pedidos.filter((p) => p.rutaCode === r.code && p.turno === turno.key));
        return (
          <div key={r.id} className="agenda-dayview__cell">
            {list.map((p) => (
              <PedidoCard key={p.id} pedido={p} ruta={r} onClick={() => onPedido(p)} />
            ))}
          </div>
        );
      })}
    </>
  );
}

const turnoShort: Record<Turno, string> = { Manana: 'Mañana', Tarde: 'Tarde', HoraEspecifica: '', CualquierHora: 'Cualquier hora' };

function PedidoCard({ pedido: p, ruta, onClick, compact }: { pedido: Pedido; ruta?: Ruta; onClick: () => void; compact?: boolean }) {
  const when = p.turno === 'HoraEspecifica' && p.specificTime ? p.specificTime.slice(0, 5) : turnoShort[p.turno];
  return (
    <button type="button" className="agenda-card" style={{ borderLeftColor: ruta?.color }} onClick={onClick}>
      <span className="agenda-card__top">
        <strong>{p.invitadoName}</strong>
        {p.priority && <span className="badge danger">Prioridad</span>}
      </span>
      <span className="muted">
        {p.zonaCode ?? 'Sin zona'} · {when}
      </span>
      {!compact && (
        <span className="muted">
          {p.lineasServicio.join(', ') || 'Sin línea'}
          {p.garmentsApprox != null && ` · ${p.garmentsApprox} prendas`}
        </span>
      )}
    </button>
  );
}

function PedidoDetail({ pedido: p, ruta, onClose }: { pedido: Pedido; ruta?: Ruta; onClose: () => void }) {
  const role = useSession().user?.role;
  const isAdmin = role === 'SuperUser' || role === 'Administracion';
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);
  const remove = useAction(
    (id: number) => unwrap(api.DELETE('/pedidos/{id}', { params: { path: { id } } })),
    [keys.agendaBoard, keys.pedidos, ['agenda']],
    'Recolección eliminada; su lugar quedó libre.',
  );

  return (
    <dialog ref={ref} onClose={onClose}>
      <div className="dialog__header">
        <h2>{p.invitadoName}</h2>
        <p className="muted">
          {formatDate(p.recoleccionDate)} · Ruta {p.rutaCode ?? '—'}
          {ruta && <span className="ruta-color" style={{ background: ruta.color, marginLeft: 6 }} />} · Zona {p.zonaCode ?? '—'}
        </p>
      </div>
      <div className="dialog__body">
        <dl className="detail-list">
          <dt>WhatsApp</dt>
          <dd>{p.whatsAppPhone}</dd>
          <dt>Dirección</dt>
          <dd>{p.address}</dd>
          <dt>Cuándo</dt>
          <dd>
            {TURNOS.find((t) => t.key === p.turno)?.name}
            {p.specificTime && ` ${p.specificTime.slice(0, 5)}`}
            {p.priority && ' · Prioridad'}
          </dd>
          <dt>Servicio</dt>
          <dd>
            {p.lineasServicio.join(', ') || '—'}
            {p.garmentsApprox != null && ` · ${p.garmentsApprox} prendas`}
          </dd>
          {p.driverNotes && (
            <>
              <dt>Notas para el chofer</dt>
              <dd>{p.driverNotes}</dd>
            </>
          )}
          <dt>Etapa</dt>
          <dd>{p.etapa}</dd>
          <dt>Origen</dt>
          <dd>{p.origin === 'App' ? 'App del invitado' : 'Oficina'}</dd>
        </dl>
      </div>
      <div className="dialog__footer">
        {isAdmin && (
          <button
            type="button"
            className="danger"
            disabled={remove.isPending}
            onClick={() => {
              if (!confirm(`¿Eliminar la recolección de ${p.invitadoName} del ${formatDate(p.recoleccionDate)}? Su lugar queda libre.`)) return;
              remove.mutate(p.id, { onSuccess: onClose });
            }}
          >
            Eliminar
          </button>
        )}
        <button type="button" className="primary" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </dialog>
  );
}

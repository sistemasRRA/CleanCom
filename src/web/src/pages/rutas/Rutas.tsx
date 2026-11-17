import { Fragment, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useOperadores } from '../../api/queries';
import { useSession } from '../../session/Session';
import { Dialog, Field, PageHeader, QueryState, optional, useAction, useSave } from '../../components/Common';
import {
  ACTIONS,
  CATEGORIES,
  PLACEMENTS,
  PasoEditor,
  TURNOS,
  categoryOf,
  emptyPaso,
  toPasoRequest,
  type PasoForm,
} from '../fijas/ParadasFijas';

type Plan = Schemas['RutaPlanDto'];
type Parada = Schemas['RutaPlanParadaDto'];
type Kind = Schemas['ParadaKind'];

const KIND: Record<Kind, string> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Entrega y recolección',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
  Salida: 'Salida',
  Regreso: 'Regreso',
  Comida: 'Comida',
  Traslado: 'Traslado',
};
const CATEGORY: Record<NonNullable<Schemas['ParadaCategory']>, string> = {
  ClienteFinal: 'Cliente final',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
};

const hm = (t?: string | null) => (t ? t.slice(0, 5) : '—');
const clockFormat = new Intl.DateTimeFormat('es-MX', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'America/Mexico_City' });
/** UTC instant from the API in Mexico City time. */
const clock = (iso?: string | null) => (iso ? clockFormat.format(new Date(iso)) : '');

function today() {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(new Date());
  return parts.slice(0, 10);
}
function addDays(iso: string, days: number) {
  const d = new Date(`${iso}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

/**
 * Rutas (docs/pantallas/oficina/rutas.md): the plan of each ruta for a day with chofer, order, estimated times,
 * lunch, return to Leones and alerts. Everything is calculated by the API.
 */
export function Rutas() {
  const [date, setDate] = useState(today);
  const plan = useQuery({
    queryKey: [...keys.rutasDia, date],
    queryFn: () => unwrap(api.GET('/rutas/dia', { params: { query: { fecha: date } } })),
    placeholderData: (previous) => previous,
  });
  const d = plan.data;

  return (
    <div className="stack">
      <PageHeader title="Rutas" description="Planeación del día por ruta: chofer, paradas en orden, horas estimadas y regreso a Leones.">
        <button type="button" className="small" onClick={() => setDate(addDays(date, -1))}>
          ◀
        </button>
        <input type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} style={{ width: 'auto' }} />
        <button type="button" className="small" onClick={() => setDate(addDays(date, 1))}>
          ▶
        </button>
        <button type="button" className="small" onClick={() => setDate(today())}>
          Hoy
        </button>
      </PageHeader>
      <QueryState loading={plan.isPending} error={plan.error}>
        {d && (
          <>
            <p className="muted rutas-note">
              <strong className="rutas-day">{d.label}</strong>
              {d.nonWorkingReason && <span className="badge warning">Día inhábil: {d.nonWorkingReason}</span>} {d.estimate}
            </p>
            {d.rutas.map((r) => (
              <RutaCard key={r.rutaId} plan={r} date={d.date} />
            ))}
          </>
        )}
      </QueryState>
    </div>
  );
}

function RutaCard({ plan: r, date }: { plan: Plan; date: string }) {
  const canEdit = useSession().can('rutas', 'edit');
  const operadores = useOperadores();
  const refetch = [keys.rutasDia, keys.agendaBoard];
  const assign = useAction(
    (operadorId: number | null) =>
      unwrap(api.PUT('/rutas/{rutaId}/dias/{fecha}/chofer', { params: { path: { rutaId: r.rutaId, fecha: date } }, body: { operadorId } })),
    refetch,
    'Chofer asignado.',
  );
  const optimize = useAction(
    () => unwrap(api.POST('/rutas/dia/{id}/optimizar', { params: { path: { id: r.rutaDiaId! } } })),
    refetch,
    `Ruta ${r.code} optimizada.`,
  );
  const patch = useAction(
    ({ id, position, fixed }: { id: number; position: number | null; fixed: boolean | null }) =>
      unwrap(api.PATCH('/paradas/{id}', { params: { path: { id } }, body: { position, fixed } })),
    refetch,
    'Orden actualizado.',
  );
  const remove = useAction(
    (id: number) => unwrap(api.DELETE('/paradas/{id}', { params: { path: { id } } })),
    refetch,
    'Quitada de la ruta.',
  );
  const [adding, setAdding] = useState(false);
  const busy = assign.isPending || optimize.isPending || patch.isPending || remove.isPending;
  const late = r.returnAt > r.maxReturn;

  return (
    <section className="card">
      <div className="card__header">
        <div className="ruta-title">
          <span className="ruta-color big" style={{ background: r.color }} />
          <div>
            <h2>{r.name}</h2>
            <div className="ruta-stats muted">
              <span className={r.occupied > r.cupo ? 'warn' : ''}>
                {r.occupied} de {r.cupo} paradas
              </span>
              <span>Sale {hm(r.departure)}</span>
              {r.lunchStart && <span>Comida {hm(r.lunchStart)}</span>}
              <span className={late ? 'warn' : ''}>
                Regresa {hm(r.returnAt)} {late && `(máx. ${hm(r.maxReturn)})`}
              </span>
              <span>{r.km} km</span>
              <span className={r.garments > r.capacity ? 'warn' : ''}>
                {r.garments} de {r.capacity} prendas
              </span>
            </div>
          </div>
        </div>
        <div className="actions">
          <label className="inline-field">
            <span className="muted">Chofer</span>
            <select
              value={r.operadorIsUsual ? '' : (r.operadorId ?? '')}
              disabled={!canEdit || busy}
              onChange={(e) => assign.mutate(e.target.value ? Number(e.target.value) : null)}
              style={{ width: 'auto' }}
            >
              <option value="">{r.operadorIsUsual ? `${r.operadorName} (habitual)` : 'Habitual de la ruta'}</option>
              {operadores.data
                ?.filter((o) => o.active)
                .map((o) => (
                  <option key={o.id} value={o.id}>
                    {o.name}
                  </option>
                ))}
            </select>
          </label>
          {canEdit && (
            <button type="button" disabled={busy} onClick={() => setAdding(true)}>
              Agregar
            </button>
          )}
          {canEdit && r.rutaDiaId && (
            <button type="button" className="primary" disabled={busy || r.paradas.length === 0} onClick={() => optimize.mutate(undefined)}>
              {optimize.isPending ? 'Optimizando…' : 'Optimizar'}
            </button>
          )}
        </div>
      </div>

      {r.alerts.length > 0 && (
        <div className="alert warning ruta-alerts">
          <ul>
            {r.alerts.map((a) => (
              <li key={a}>{a}</li>
            ))}
          </ul>
        </div>
      )}

      {r.paradas.length === 0 ? (
        <div className="empty">Sin paradas este día.</div>
      ) : (
        <div className="table-wrap">
          <table className="ruta-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Hora</th>
                <th>Parada</th>
                <th>Qué se hace</th>
                <th>Ventana</th>
                <th>Km</th>
                {canEdit && <th></th>}
              </tr>
            </thead>
            <tbody>
              <tr className="ruta-base">
                <td></td>
                <td className="num">{hm(r.departure)}</td>
                <td colSpan={canEdit ? 5 : 4}>Salida de Leones (carga de la unidad)</td>
              </tr>
              {r.paradas.map((p, i) => (
                <Fragment key={p.id}>
                  <ParadaRow
                    p={p}
                    first={i === 0}
                    last={i === r.paradas.length - 1}
                    canEdit={canEdit}
                    busy={busy}
                    onMove={(by) => patch.mutate({ id: p.id, position: p.order + by, fixed: null })}
                    onPin={() => patch.mutate({ id: p.id, position: null, fixed: !p.positionFixed })}
                    onRemove={() =>
                      confirm(p.viaje ? `¿Quitar todo el viaje "${p.viaje.split(' · ')[0]}" de la ruta este día?` : `¿Quitar "${p.name}" de la ruta este día?`) &&
                      remove.mutate(p.id)
                    }
                  />
                  {p.lunchAfter && (
                    <tr className="ruta-lunch">
                      <td></td>
                      <td className="num">{hm(p.leave)}</td>
                      <td colSpan={canEdit ? 5 : 4}>Comida del chofer</td>
                    </tr>
                  )}
                </Fragment>
              ))}
              <tr className={`ruta-base${late ? ' late' : ''}`}>
                <td></td>
                <td className="num">{hm(r.returnAt)}</td>
                <td colSpan={canEdit ? 5 : 4}>Entrega de ruta en Leones (descarga)</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
      {adding && <AddDialog ruta={r} date={date} onClose={() => setAdding(false)} />}
    </section>
  );
}

function ParadaRow({
  p,
  first,
  last,
  canEdit,
  busy,
  onMove,
  onPin,
  onRemove,
}: {
  p: Parada;
  first: boolean;
  last: boolean;
  canEdit: boolean;
  busy: boolean;
  onMove: (by: number) => void;
  onPin: () => void;
  onRemove: () => void;
}) {
  return (
    <tr className={p.outOfWindow ? 'late' : ''}>
      <td className="num">
        <span className="ruta-order">{p.order}</span>
      </td>
      <td className="num">
        <strong>{hm(p.arrival)}</strong>
        {p.waitMinutes > 0 && <div className="muted">espera {p.waitMinutes} min</div>}
      </td>
      <td>
        <strong>{p.name}</strong>
        {p.priority && <span className="badge danger ruta-flag">Prioridad</span>}
        {p.placement === 'Primera' && <span className="badge brand ruta-flag">1ª</span>}
        {p.placement === 'Ultima' && <span className="badge brand ruta-flag">Últ.</span>}
        {p.viaje && <div className="ruta-viaje">{p.viaje}</div>}
        {p.activity && <div>{p.activity}</div>}
        <div className="muted">
          {p.address ?? 'Sin dirección'}
          {p.zonaCode && ` · ${p.zonaCode}`}
        </div>
        {p.notes && <div className="muted">Nota: {p.notes}</div>}
        {p.noLocation && <div className="badge warning ruta-flag">Sin coordenadas</div>}
        {p.state === 'EnCamino' && <div className="badge brand ruta-flag">En camino desde las {clock(p.enCaminoAt)}</div>}
        {p.state === 'EnSitio' && (
          <div className={`badge ${p.arrivedOutsideRadius ? 'warning' : 'success'} ruta-flag`}>
            Llegó a las {clock(p.arrivedAt)}
            {p.arrivedOutsideRadius && ' · fuera de radio'}
          </div>
        )}
      </td>
      <td>
        {p.category && p.category !== 'ClienteFinal' && <div className="muted">{CATEGORY[p.category]}</div>}
        {KIND[p.kind]}
        {p.garmentsApprox != null && <div className="muted">{p.garmentsApprox} prendas</div>}
      </td>
      <td className="num">
        {hm(p.windowStart)} a {hm(p.windowEnd)}
        {p.outOfWindow && <div className="badge danger ruta-flag">Fuera de ventana</div>}
      </td>
      <td className="num muted">{p.kmFromPrevious}</td>
      {canEdit && (
        <td className="end">
          <div className="actions tight end">
            <button type="button" className="small" disabled={busy || first} onClick={() => onMove(-1)} title="Subir">
              ▲
            </button>
            <button type="button" className="small" disabled={busy || last} onClick={() => onMove(1)} title="Bajar">
              ▼
            </button>
            <button
              type="button"
              className={`small${p.positionFixed ? ' selected' : ''}`}
              disabled={busy}
              onClick={onPin}
              title={p.positionFixed ? 'Fija: optimizar no la mueve. Clic para soltarla.' : 'Fijar en esta posición'}
            >
              {p.positionFixed ? '📌 Fija' : 'Fijar'}
            </button>
            {p.pedidoId == null && p.state === 'Pendiente' && (
              <button type="button" className="small danger" disabled={busy} onClick={onRemove} title="Quitar de la ruta este día">
                Quitar
              </button>
            )}
          </div>
        </td>
      )}
    </tr>
  );
}

type AddKind = 'parada' | 'viaje';

/** "Agregar": parada de un día (one lugar) or viaje de un día (pasos in order), only for this date. */
function AddDialog({ ruta, date, onClose }: { ruta: Plan; date: string; onClose: () => void }) {
  const lugares = useQuery({ queryKey: keys.lugares, queryFn: () => unwrap(api.GET('/lugares')) });
  const [type, setType] = useState<AddKind>('parada');
  const [f, setF] = useState({
    name: '',
    category: 'Insumos' as NonNullable<Schemas['ParadaCategory']>,
    kind: 'Entrega' as Kind,
    turno: 'CualquierHora' as Schemas['Turno'],
    placement: 'Automatica' as Schemas['ParadaPlacement'],
    carried: '',
    driverNotes: '',
  });
  const [pasos, setPasos] = useState<PasoForm[]>([{ ...emptyPaso }]);
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const changePaso = (i: number, c: Partial<PasoForm>) => setPasos((list) => list.map((p, j) => (j === i ? { ...p, ...c } : p)));
  const move = (i: number, by: number) =>
    setPasos((list) => {
      const next = [...list];
      [next[i], next[i + by]] = [next[i + by], next[i]];
      return next;
    });
  const switchType = (t: AddKind) => {
    setType(t);
    setPasos((list) => (t === 'viaje' ? (list.length < 2 ? [...list, { ...emptyPaso }] : list) : list.slice(0, 1)));
    if (t === 'viaje') change({ category: 'Traspaso', kind: 'Traslado' });
  };

  const save = useSave(
    () => {
      const body = {
        rutaId: ruta.rutaId,
        date,
        name: f.name.trim(),
        category: f.category,
        kind: f.kind,
        turno: f.turno,
        placement: f.placement,
        carried: optional(f.carried),
        driverNotes: optional(f.driverNotes),
        pasos: pasos.map(toPasoRequest),
      };
      return type === 'viaje' ? unwrap(api.POST('/viajes', { body })) : unwrap(api.POST('/paradas', { body }));
    },
    { refetch: [keys.rutasDia, keys.agendaBoard], message: type === 'viaje' ? 'Viaje agregado a la ruta.' : 'Parada agregada a la ruta.', onDone: onClose },
  );

  return (
    <Dialog
      title={`Agregar a la ruta ${ruta.code}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      submitText="Agregar"
      error={save.error}
    >
      <div className="segmented">
        <button type="button" className={`segmented__option${type === 'parada' ? ' selected' : ''}`} onClick={() => switchType('parada')}>
          Parada de un día
        </button>
        <button type="button" className={`segmented__option${type === 'viaje' ? ' selected' : ''}`} onClick={() => switchType('viaje')}>
          Viaje de un día
        </button>
      </div>
      <p className="muted">
        {type === 'parada'
          ? 'Insumos, traspaso o una visita que solo es este día. Ocupa un lugar del cupo.'
          : 'Varios lugares que se visitan en orden (entre tiendas o desde bodega). Cada paso ocupa un lugar del cupo.'}
      </p>
      <div className="form-grid">
        <Field label="Qué es" wide>
          <input
            autoFocus
            value={f.name}
            onChange={(e) => change({ name: e.target.value })}
            required
            placeholder={type === 'viaje' ? 'Viaje de bodega a Polanco' : 'Insumos Florida'}
          />
        </Field>
        <Field label="Tipo de parada" hint={categoryOf(f.category).hint}>
          <select value={f.category} onChange={(e) => change({ category: e.target.value as typeof f.category })}>
            {CATEGORIES.map((c) => (
              <option key={c.key} value={c.key}>
                {c.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Qué se hace">
          <select value={f.kind} onChange={(e) => change({ kind: e.target.value as Kind })}>
            {ACTIONS.map((a) => (
              <option key={a.key} value={a.key}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Turno">
          <select value={f.turno} onChange={(e) => change({ turno: e.target.value as typeof f.turno })}>
            {TURNOS.map((t) => (
              <option key={t.key} value={t.key}>
                {t.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Lugar en la ruta">
          <select value={f.placement} onChange={(e) => change({ placement: e.target.value as typeof f.placement })}>
            {PLACEMENTS.map((p) => (
              <option key={p.key} value={p.key}>
                {p.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label={type === 'viaje' ? 'Qué se lleva' : 'Insumos o carga'} wide>
          <input value={f.carried} onChange={(e) => change({ carried: e.target.value })} placeholder="2 galones de 10 L, 1 caja de ganchos" />
        </Field>
        <Field label="Notas para el chofer" wide>
          <input value={f.driverNotes} onChange={(e) => change({ driverNotes: e.target.value })} />
        </Field>
      </div>

      <h3 className="form-section">{type === 'viaje' ? `Pasos (${pasos.length}), en orden` : 'Dónde'}</h3>
      <div className="pasos">
        {pasos.map((p, i) => (
          <PasoEditor
            key={i}
            index={i}
            count={pasos.length}
            paso={p}
            lugares={lugares.data ?? []}
            defaultKind={f.kind}
            defaultTurno={f.turno}
            onChange={(c) => changePaso(i, c)}
            onMove={(by) => move(i, by)}
            onRemove={() => setPasos((list) => list.filter((_, j) => j !== i))}
          />
        ))}
      </div>
      {type === 'viaje' && (
        <div className="actions tight">
          <button type="button" className="small" onClick={() => setPasos((list) => [...list, { ...emptyPaso }])} disabled={pasos.length >= 10}>
            + Otro paso
          </button>
          <button
            type="button"
            className="small"
            disabled={pasos.length >= 10 || !(pasos[0].lugarId || pasos[0].address)}
            onClick={() => setPasos((list) => [...list, { ...list[0], activity: 'Regresar', notes: '', before: '' }])}
          >
            + Regresar al inicio
          </button>
        </div>
      )}
    </Dialog>
  );
}

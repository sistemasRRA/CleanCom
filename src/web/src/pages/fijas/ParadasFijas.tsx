import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useRutas } from '../../api/queries';
import { useSession } from '../../session/Session';
import { useNotify } from '../../components/Toasts';
import {
  Checkbox,
  Dialog,
  Field,
  PageHeader,
  QueryState,
  WEEKDAYS,
  optional,
  optionalNumber,
  useAction,
  useSave,
  type Weekday,
} from '../../components/Common';

type Fija = Schemas['ParadaFijaDto'];
type Kind = Schemas['ParadaKind'];
type Turno = Schemas['Turno'];
type Placement = Schemas['ParadaPlacement'];
type Lugar = Schemas['LugarDto'];

/** Kinds a parada fija can have, with the name and color class used on screen. */
export const FIJA_KINDS: { key: Kind; name: string; tone: string }[] = [
  { key: 'Convenio', name: 'Convenio', tone: 'kind-convenio' },
  { key: 'Insumos', name: 'Insumos', tone: 'kind-insumos' },
  { key: 'Traspaso', name: 'Traspaso', tone: 'kind-traspaso' },
  { key: 'Recoleccion', name: 'Cliente final: recolección', tone: 'kind-cliente' },
  { key: 'Entrega', name: 'Cliente final: entrega', tone: 'kind-cliente' },
  { key: 'RecoleccionYEntrega', name: 'Cliente final: entrega y recolección', tone: 'kind-cliente' },
];
const kindOf = (k: Kind) => FIJA_KINDS.find((x) => x.key === k) ?? { key: k, name: k, tone: '' };

const TURNOS: { key: Turno; name: string }[] = [
  { key: 'Manana', name: 'Mañana' },
  { key: 'Tarde', name: 'Tarde' },
  { key: 'CualquierHora', name: 'Cualquier hora' },
];
const turnoName = (t: Turno) => TURNOS.find((x) => x.key === t)?.name ?? t;

const PLACEMENTS: { key: Placement; name: string; short: string }[] = [
  { key: 'Automatica', name: 'Automático (lo decide el ruteo)', short: '' },
  { key: 'Primera', name: 'Primera de la ruta', short: '1ª' },
  { key: 'Ultima', name: 'Última de la ruta', short: 'Últ.' },
];
const placementName = (p: Placement) => PLACEMENTS.find((x) => x.key === p)?.name.split(' (')[0] ?? p;

const dayShort = (d: Weekday) => WEEKDAYS.find((x) => x.key === d)?.name.slice(0, 2) ?? d;

/** yyyy-MM-dd plus days, without time zone surprises. */
function addDays(iso: string, days: number) {
  const d = new Date(`${iso}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}
const shortDate = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short', timeZone: 'UTC' });
const short = (iso: string) => shortDate.format(new Date(`${iso}T00:00:00Z`)).replace('.', '');

/**
 * Paradas fijas (docs/pantallas/oficina/paradas-fijas.md): convenios, insumos and traspasos that repeat every week.
 * A vuelta fija is one record with several pasos. The API generates them every night for the days ahead and they take cupo.
 */
export function ParadasFijas() {
  const canEdit = useSession().can('paradas-fijas', 'edit');
  const [kind, setKind] = useState<Kind | ''>('');
  const [inactive, setInactive] = useState(false);
  const [editing, setEditing] = useState<Fija | 'new' | null>(null);

  const list = useQuery({
    queryKey: [...keys.paradasFijas, 'lista', kind, inactive],
    queryFn: () => unwrap(api.GET('/paradas-fijas', { params: { query: { tipo: kind || undefined, inactivas: inactive } } })),
    placeholderData: (previous) => previous,
  });
  const open = async (id: number) => setEditing(await unwrap(api.GET('/paradas-fijas/{id}', { params: { path: { id } } })));

  return (
    <div className="stack">
      <PageHeader title="Paradas fijas" description="El trabajo que se repite cada semana. Se generan solas cada noche para los próximos días y cuentan dentro del cupo de cada ruta.">
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva parada fija
          </button>
        )}
      </PageHeader>

      <WeekGrid onOpen={open} canEdit={canEdit} />

      <section className="card">
        <div className="card__header">
          <h2>Todas las paradas fijas</h2>
          <div className="actions">
            <select value={kind} onChange={(e) => setKind(e.target.value as Kind | '')} style={{ width: 'auto' }}>
              <option value="">Todos los tipos</option>
              {FIJA_KINDS.map((k) => (
                <option key={k.key} value={k.key}>
                  {k.name}
                </option>
              ))}
            </select>
            <Checkbox checked={inactive} onChange={setInactive}>
              Ver inactivas
            </Checkbox>
          </div>
        </div>
        <QueryState loading={list.isPending} error={list.error}>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Parada</th>
                  <th>Tipo</th>
                  <th>Ruta</th>
                  <th>Días</th>
                  <th>Turno</th>
                  <th>Lugar en la ruta</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {list.data?.map((f) => (
                  <tr key={f.id} className={f.active ? '' : 'inactive'}>
                    <td>
                      <strong>{f.name}</strong>
                      <div className="muted fija-pasos">{f.pasos.map((p) => p.lugarName ?? p.address).join(' → ')}</div>
                    </td>
                    <td>
                      <span className={`badge ${kindOf(f.kind).tone}`}>{kindOf(f.kind).name}</span>
                      {f.isVuelta && <div className="muted">Vuelta de {f.pasos.length} pasos</div>}
                    </td>
                    <td>
                      <span className="ruta-color" style={{ background: f.rutaColor }} />
                      {f.rutaCode}
                    </td>
                    <td className="muted">{WEEKDAYS.filter((d) => f.days.includes(d.key)).map((d) => dayShort(d.key)).join(' ')}</td>
                    <td>{turnoName(f.turno)}</td>
                    <td>{placementName(f.placement)}</td>
                    <td>
                      <FijaState fija={f} />
                    </td>
                    <td className="end">
                      <button type="button" className="small" onClick={() => open(f.id)}>
                        {canEdit ? 'Editar' : 'Ver'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {list.data?.length === 0 && <div className="empty">Sin paradas fijas con estos filtros.</div>}
          </div>
        </QueryState>
      </section>

      {editing && <FijaForm fija={editing === 'new' ? null : editing} canEdit={canEdit} onClose={() => setEditing(null)} />}
    </div>
  );
}

function FijaState({ fija }: { fija: Fija }) {
  if (!fija.active) return <span className="badge">Inactiva</span>;
  if (fija.paused) return <span className="badge warning">Pausada</span>;
  return (
    <>
      <span className="badge success">Activa</span>
      {(fija.validFrom || fija.validUntil) && (
        <div className="muted">
          {fija.validFrom ? `desde ${short(fija.validFrom)}` : ''} {fija.validUntil ? `hasta ${short(fija.validUntil)}` : ''}
        </div>
      )}
    </>
  );
}

/** "La semana de un vistazo": rutas by day, Monday to Saturday, with the cupo the fijas take. */
function WeekGrid({ onOpen, canEdit }: { onOpen: (id: number) => void; canEdit: boolean }) {
  const [date, setDate] = useState<string | null>(null);
  const [regenerating, setRegenerating] = useState('');
  const notify = useNotify();
  const week = useQuery({
    queryKey: [...keys.paradasFijas, 'semana', date],
    queryFn: () => unwrap(api.GET('/paradas-fijas/semana', { params: { query: { fecha: date ?? undefined } } })),
    placeholderData: (previous) => previous,
  });
  const regenerate = useAction(
    (fecha: string) =>
      unwrap(api.POST('/paradas-fijas/generar', { params: { query: { fecha } } })).then((r) => {
        notify(r.message);
        setRegenerating('');
      }),
    [keys.paradasFijas, keys.agendaBoard],
    'Listo.',
  );
  const w = week.data;

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>La semana de un vistazo</h2>
          {w && (
            <p className="muted">
              Del {short(w.from)} al {short(w.to)}. Cada paso ocupa un lugar del cupo.
            </p>
          )}
        </div>
        <div className="actions">
          <button type="button" className="small" onClick={() => w && setDate(addDays(w.from, -7))}>
            ◀ Anterior
          </button>
          <button type="button" className="small" onClick={() => setDate(null)}>
            Esta semana
          </button>
          <button type="button" className="small" onClick={() => w && setDate(addDays(w.from, 7))}>
            Siguiente ▶
          </button>
          {canEdit && (
            <span className="actions tight">
              <input type="date" value={regenerating} onChange={(e) => setRegenerating(e.target.value)} style={{ width: 'auto' }} title="Día a regenerar" />
              <button
                type="button"
                className="small"
                disabled={!regenerating || regenerate.isPending}
                onClick={() => regenerate.mutate(regenerating)}
                title="Reemplaza las paradas fijas de ese día que el chofer no ha iniciado"
              >
                Agregar las fijas del día
              </button>
            </span>
          )}
        </div>
      </div>
      <QueryState loading={week.isPending} error={week.error}>
        {w && (
          <div className="table-wrap">
            <table className="fijas-week">
              <thead>
                <tr>
                  <th>Ruta</th>
                  {w.days.map((d) => (
                    <th key={d.date} className={d.nonWorkingReason ? 'off' : ''}>
                      {d.label}
                      {d.nonWorkingReason && <div className="muted">Inhábil</div>}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {w.rutas.map((r) => (
                  <tr key={r.id}>
                    <th>
                      <span className="ruta-color" style={{ background: r.color }} />
                      {r.code}
                    </th>
                    {w.days.map((d) => {
                      const cell = d.rutas.find((c) => c.rutaId === r.id);
                      return (
                        <td key={d.date} className={d.nonWorkingReason ? 'off' : ''}>
                          {d.nonWorkingReason ? (
                            <span className="muted">{d.nonWorkingReason}</span>
                          ) : (
                            <div className="fijas-cell">
                              {cell?.items.map((i) => (
                                <button
                                  key={i.paradaFijaId}
                                  type="button"
                                  className={`fija-chip ${kindOf(i.kind).tone}`}
                                  onClick={() => onOpen(i.paradaFijaId)}
                                  title={`${kindOf(i.kind).name} · ${turnoName(i.turno)} · ${i.pasos} lugar(es)${i.generated ? ' · ya está en la ruta' : ''}`}
                                >
                                  {PLACEMENTS.find((p) => p.key === i.placement)?.short && (
                                    <b>{PLACEMENTS.find((p) => p.key === i.placement)?.short} </b>
                                  )}
                                  {i.name}
                                  <small> · {turnoName(i.turno)}</small>
                                </button>
                              ))}
                              {cell && (
                                <span className={`muted fijas-cupo${cell.fijasCupo > cell.cupo / 2 ? ' warn' : ''}`}>
                                  {cell.fijasCupo} de {cell.cupo} lugares
                                </span>
                              )}
                            </div>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </QueryState>
    </section>
  );
}

type PasoForm = { lugarId: string; address: string; lat: string; lng: string; activity: string; notes: string };
const emptyPaso: PasoForm = { lugarId: '', address: '', lat: '', lng: '', activity: '', notes: '' };

function FijaForm({ fija, canEdit, onClose }: { fija: Fija | null; canEdit: boolean; onClose: () => void }) {
  const rutas = useRutas();
  const lugares = useQuery({ queryKey: keys.lugares, queryFn: () => unwrap(api.GET('/lugares')) });
  const [f, setF] = useState({
    name: fija?.name ?? '',
    kind: fija?.kind ?? ('Convenio' as Kind),
    rutaId: fija?.rutaId?.toString() ?? '',
    turno: fija?.turno ?? ('CualquierHora' as Turno),
    placement: fija?.placement ?? ('Automatica' as Placement),
    days: (fija?.days ?? []) as Weekday[],
    carried: fija?.carried ?? '',
    siteContact: fija?.siteContact ?? '',
    sitePhone: fija?.sitePhone ?? '',
    garmentsApprox: fija?.garmentsApprox?.toString() ?? '',
    driverNotes: fija?.driverNotes ?? '',
    validFrom: fija?.validFrom ?? '',
    validUntil: fija?.validUntil ?? '',
    paused: fija?.paused ?? false,
    active: fija?.active ?? true,
  });
  const [pasos, setPasos] = useState<PasoForm[]>(
    fija?.pasos.map((p) => ({
      lugarId: p.lugarId?.toString() ?? '',
      address: p.lugarId ? '' : (p.address ?? ''),
      lat: p.lugarId ? '' : (p.lat?.toString() ?? ''),
      lng: p.lugarId ? '' : (p.lng?.toString() ?? ''),
      activity: p.activity ?? '',
      notes: p.notes ?? '',
    })) ?? [{ ...emptyPaso }],
  );
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const changePaso = (i: number, c: Partial<PasoForm>) => setPasos((list) => list.map((p, j) => (j === i ? { ...p, ...c } : p)));
  const move = (i: number, by: number) =>
    setPasos((list) => {
      const next = [...list];
      [next[i], next[i + by]] = [next[i + by], next[i]];
      return next;
    });

  const save = useSave(
    () => {
      const body = {
        name: f.name.trim(),
        kind: f.kind,
        rutaId: Number(f.rutaId),
        turno: f.turno,
        placement: f.placement,
        days: f.days,
        carried: optional(f.carried),
        siteContact: optional(f.siteContact),
        sitePhone: optional(f.sitePhone),
        garmentsApprox: optionalNumber(f.garmentsApprox),
        driverNotes: optional(f.driverNotes),
        validFrom: f.validFrom || null,
        validUntil: f.validUntil || null,
        paused: f.paused,
        active: f.active,
        pasos: pasos.map((p) => ({
          lugarId: p.lugarId ? Number(p.lugarId) : null,
          address: p.lugarId ? null : optional(p.address),
          lat: p.lugarId ? null : optionalNumber(p.lat),
          lng: p.lugarId ? null : optionalNumber(p.lng),
          activity: optional(p.activity),
          notes: optional(p.notes),
        })),
      };
      return fija
        ? unwrap(api.PUT('/paradas-fijas/{id}', { params: { path: { id: fija.id } }, body }))
        : unwrap(api.POST('/paradas-fijas', { body }));
    },
    {
      refetch: [keys.paradasFijas, keys.agendaBoard],
      message: fija ? 'Parada fija guardada; sus paradas de los próximos días se actualizaron.' : 'Parada fija creada y agregada a los próximos días.',
      onDone: onClose,
    },
  );

  return (
    <Dialog
      title={fija?.name ?? 'Nueva parada fija'}
      onClose={onClose}
      onSubmit={() => (canEdit ? save.mutate(undefined) : onClose())}
      busy={save.isPending}
      submitText={canEdit ? 'Guardar' : 'Cerrar'}
      error={save.error}
    >
      <fieldset disabled={!canEdit} className="plain">
        <div className="form-grid">
          <Field label="Nombre" wide>
            <input autoFocus value={f.name} onChange={(e) => change({ name: e.target.value })} required placeholder="Traspaso K, Insumos Florida, Convenio Wildress" />
          </Field>
          <Field label="Tipo de parada">
            <select value={f.kind} onChange={(e) => change({ kind: e.target.value as Kind })}>
              {FIJA_KINDS.map((k) => (
                <option key={k.key} value={k.key}>
                  {k.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Ruta">
            <select value={f.rutaId} onChange={(e) => change({ rutaId: e.target.value })} required>
              <option value="">Elige la ruta</option>
              {rutas.data
                ?.filter((r) => r.active || r.id === fija?.rutaId)
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    Ruta {r.code}
                  </option>
                ))}
            </select>
          </Field>
          <Field label="Turno">
            <select value={f.turno} onChange={(e) => change({ turno: e.target.value as Turno })}>
              {TURNOS.map((t) => (
                <option key={t.key} value={t.key}>
                  {t.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Lugar en la ruta">
            <select value={f.placement} onChange={(e) => change({ placement: e.target.value as Placement })}>
              {PLACEMENTS.map((p) => (
                <option key={p.key} value={p.key}>
                  {p.name}
                </option>
              ))}
            </select>
          </Field>
          <div className="field wide">
            <span>Días</span>
            <div className="weekday-picker">
              {WEEKDAYS.map((d) => (
                <Checkbox
                  key={d.key}
                  checked={f.days.includes(d.key)}
                  onChange={(on) => change({ days: on ? [...f.days, d.key] : f.days.filter((x) => x !== d.key) })}
                >
                  {d.name}
                </Checkbox>
              ))}
            </div>
          </div>
        </div>

        <h3 className="form-section">{pasos.length > 1 ? `Pasos de la vuelta (${pasos.length}), en orden` : 'Dónde'}</h3>
        <p className="muted">
          Un paso es una parada fija; con varios pasos es una vuelta y se visitan seguidos y en orden. Cada paso ocupa un lugar del cupo.
        </p>
        <div className="pasos">
          {pasos.map((p, i) => (
            <PasoEditor
              key={i}
              index={i}
              count={pasos.length}
              paso={p}
              lugares={lugares.data ?? []}
              onChange={(c) => changePaso(i, c)}
              onMove={(by) => move(i, by)}
              onRemove={() => setPasos((list) => list.filter((_, j) => j !== i))}
            />
          ))}
        </div>
        {canEdit && (
          <div className="actions tight">
            <button type="button" className="small" onClick={() => setPasos((list) => [...list, { ...emptyPaso }])} disabled={pasos.length >= 10}>
              + Otro paso
            </button>
            <button
              type="button"
              className="small"
              disabled={pasos.length >= 10 || !(pasos[0].lugarId || pasos[0].address)}
              onClick={() => setPasos((list) => [...list, { ...list[0], activity: 'Regresar', notes: '' }])}
            >
              + Regresar al inicio
            </button>
          </div>
        )}

        <h3 className="form-section">Detalle</h3>
        <div className="form-grid">
          <Field label="Insumos o carga" wide hint="Texto libre; no hay inventario.">
            <input value={f.carried} onChange={(e) => change({ carried: e.target.value })} />
          </Field>
          <Field label="Contacto en sitio">
            <input value={f.siteContact} onChange={(e) => change({ siteContact: e.target.value })} />
          </Field>
          <Field label="Teléfono">
            <input inputMode="tel" value={f.sitePhone} onChange={(e) => change({ sitePhone: e.target.value })} />
          </Field>
          <Field label="Prendas aproximadas">
            <input inputMode="numeric" value={f.garmentsApprox} onChange={(e) => change({ garmentsApprox: e.target.value })} />
          </Field>
          <Field label="Notas para el chofer" wide>
            <input value={f.driverNotes} onChange={(e) => change({ driverNotes: e.target.value })} />
          </Field>
          <Field label="Vigente desde" hint="Vacío: ya está vigente.">
            <input type="date" value={f.validFrom} onChange={(e) => change({ validFrom: e.target.value })} />
          </Field>
          <Field label="Vigente hasta" hint="Vacío: sin fin.">
            <input type="date" value={f.validUntil} onChange={(e) => change({ validUntil: e.target.value })} />
          </Field>
          <Checkbox checked={f.paused} onChange={(paused) => change({ paused })}>
            Pausada (no se genera, sin desactivarla)
          </Checkbox>
          <Checkbox checked={f.active} onChange={(active) => change({ active })}>
            Activa
          </Checkbox>
        </div>
      </fieldset>

      {fija && <Exceptions fija={fija} canEdit={canEdit} />}
    </Dialog>
  );
}

function PasoEditor({
  index,
  count,
  paso,
  lugares,
  onChange,
  onMove,
  onRemove,
}: {
  index: number;
  count: number;
  paso: PasoForm;
  lugares: Lugar[];
  onChange: (c: Partial<PasoForm>) => void;
  onMove: (by: number) => void;
  onRemove: () => void;
}) {
  return (
    <div className="paso">
      <div className="paso__number">{index + 1}</div>
      <div className="form-grid">
        <Field label="Lugar del catálogo">
          <select value={paso.lugarId} onChange={(e) => onChange({ lugarId: e.target.value })}>
            <option value="">Otra dirección…</option>
            {lugares.map((l) => (
              <option key={l.id} value={l.id}>
                {l.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Qué se hace">
          <input value={paso.activity} onChange={(e) => onChange({ activity: e.target.value })} placeholder="Cargar, entregar insumos…" />
        </Field>
        {!paso.lugarId && (
          <>
            <Field label="Dirección" wide>
              <input value={paso.address} onChange={(e) => onChange({ address: e.target.value })} required />
            </Field>
            <Field label="Latitud" hint="Para el mapa y el ruteo.">
              <input inputMode="decimal" value={paso.lat} onChange={(e) => onChange({ lat: e.target.value })} />
            </Field>
            <Field label="Longitud">
              <input inputMode="decimal" value={paso.lng} onChange={(e) => onChange({ lng: e.target.value })} />
            </Field>
          </>
        )}
        <Field label="Nota" wide>
          <input value={paso.notes} onChange={(e) => onChange({ notes: e.target.value })} />
        </Field>
      </div>
      <div className="paso__actions">
        <button type="button" className="small" disabled={index === 0} onClick={() => onMove(-1)} title="Subir">
          ▲
        </button>
        <button type="button" className="small" disabled={index === count - 1} onClick={() => onMove(1)} title="Bajar">
          ▼
        </button>
        <button type="button" className="small danger" disabled={count === 1} onClick={onRemove} title="Quitar paso">
          ✕
        </button>
      </div>
    </div>
  );
}

/** Dates the parada fija is skipped. */
function Exceptions({ fija, canEdit }: { fija: Fija; canEdit: boolean }) {
  const [current, setCurrent] = useState(fija.exceptions);
  const [date, setDate] = useState('');
  const [reason, setReason] = useState('');
  const notify = useNotify();
  const add = useAction(
    () =>
      unwrap(api.POST('/paradas-fijas/{id}/excepciones', { params: { path: { id: fija.id } }, body: { date, reason: optional(reason) } })).then((r) => {
        setCurrent(r.exceptions);
        setDate('');
        setReason('');
      }),
    [keys.paradasFijas, keys.agendaBoard],
    'Fecha saltada; ese día sale de la ruta.',
  );
  const remove = useAction(
    (exceptionId: number) =>
      unwrap(api.DELETE('/paradas-fijas/{id}/excepciones/{exceptionId}', { params: { path: { id: fija.id, exceptionId } } })).then((r) =>
        setCurrent(r.exceptions),
      ),
    [keys.paradasFijas, keys.agendaBoard],
    'Excepción quitada.',
  );

  return (
    <>
      <h3 className="form-section">Saltar fechas</h3>
      {current.length === 0 ? (
        <p className="muted">Sin fechas saltadas próximas.</p>
      ) : (
        <ul className="plain-list">
          {current.map((e) => (
            <li key={e.id}>
              <strong>{e.label}</strong> {e.reason && <span className="muted">· {e.reason}</span>}
              {canEdit && (
                <button type="button" className="small link" disabled={remove.isPending} onClick={() => remove.mutate(e.id)}>
                  Quitar
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
      {canEdit && (
        <div className="actions tight">
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} style={{ width: 'auto' }} />
          <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Motivo (opcional)" style={{ maxWidth: 220 }} />
          <button
            type="button"
            className="small"
            disabled={!date || add.isPending}
            onClick={() => (date ? add.mutate(undefined) : notify('Elige la fecha.', true))}
          >
            Saltar esta fecha
          </button>
        </div>
      )}
    </>
  );
}

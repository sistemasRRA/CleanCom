import { useState } from 'react';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useRutas, useZonas } from '../../api/queries';
import {
  ActiveBadge,
  Checkbox,
  Dialog,
  Field,
  QueryState,
  WEEKDAYS,
  optionalNumber,
  useAction,
  useSave,
  type Weekday,
} from '../../components/Common';
import { useSession } from '../../session/Session';

type Ruta = Schemas['RutaDto'];
type Zona = Schemas['ZonaDto'];

// ---------------- Rutas ----------------

export function Rutas() {
  const canEdit = useSession().can('operacion', 'edit');
  const rutas = useRutas();
  const [editing, setEditing] = useState<Ruta | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Rutas</h2>
          <p className="muted">Vacío en cupo, meta o regreso = usa la regla general de Operación.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva ruta
          </button>
        )}
      </div>
      <QueryState loading={rutas.isPending} error={rutas.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Código</th>
                <th>Nombre</th>
                <th>Cupo</th>
                <th>Meta</th>
                <th>Regreso</th>
                <th>Regreso sábado</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rutas.data?.map((r) => (
                <tr key={r.id} className={r.active ? '' : 'inactive'}>
                  <td>
                    <span className="ruta-color" style={{ background: r.color }} />
                    <strong>{r.code}</strong>
                  </td>
                  <td>{r.name}</td>
                  <td className="num">{r.cupoParadas ?? <span className="muted">general</span>}</td>
                  <td className="num">{r.recoleccionGoal ?? <span className="muted">general</span>}</td>
                  <td className="num">{shortTime(r.maxReturnTime) ?? <span className="muted">general</span>}</td>
                  <td className="num">{shortTime(r.maxReturnTimeSaturday) ?? <span className="muted">general</span>}</td>
                  <td>
                    <ActiveBadge active={r.active} />
                  </td>
                  <td>
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(r)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {rutas.data?.length === 0 && <div className="empty">Todavía no hay rutas.</div>}
        </div>
      </QueryState>
      {editing && <RutaForm ruta={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

/** "13:00:00" → "13:00". */
function shortTime(value?: string | null) {
  return value ? value.slice(0, 5) : null;
}

function RutaForm({ ruta, onClose }: { ruta: Ruta | null; onClose: () => void }) {
  const [f, setF] = useState({
    code: ruta?.code ?? '',
    name: ruta?.name ?? '',
    color: ruta?.color ?? '#2a9d8f',
    cupo: ruta?.cupoParadas?.toString() ?? '',
    goal: ruta?.recoleccionGoal?.toString() ?? '',
    maxReturn: shortTime(ruta?.maxReturnTime) ?? '',
    maxReturnSaturday: shortTime(ruta?.maxReturnTimeSaturday) ?? '',
    active: ruta?.active ?? true,
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));

  const save = useSave(
    () => {
      const body = {
        code: f.code.trim(),
        name: f.name.trim(),
        color: f.color,
        cupoParadas: optionalNumber(f.cupo),
        recoleccionGoal: optionalNumber(f.goal),
        maxReturnTime: f.maxReturn ? `${f.maxReturn}:00` : null,
        maxReturnTimeSaturday: f.maxReturnSaturday ? `${f.maxReturnSaturday}:00` : null,
        active: f.active,
      };
      return ruta
        ? unwrap(api.PUT('/rutas/{id}', { params: { path: { id: ruta.id } }, body }))
        : unwrap(api.POST('/rutas', { body }));
    },
    { refetch: [keys.rutas, keys.zonas], message: ruta ? 'Ruta actualizada.' : 'Ruta creada.', onDone: onClose },
  );

  return (
    <Dialog title={ruta ? `Ruta ${ruta.code}` : 'Nueva ruta'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Código">
          <input autoFocus value={f.code} onChange={(e) => change({ code: e.target.value })} required />
        </Field>
        <Field label="Nombre">
          <input value={f.name} onChange={(e) => change({ name: e.target.value })} required />
        </Field>
        <Field label="Color">
          <input type="color" value={f.color} onChange={(e) => change({ color: e.target.value })} />
        </Field>
        <Field label="Cupo de paradas">
          <input type="number" min={1} value={f.cupo} onChange={(e) => change({ cupo: e.target.value })} placeholder="general" />
        </Field>
        <Field label="Meta de recolecciones">
          <input type="number" min={1} value={f.goal} onChange={(e) => change({ goal: e.target.value })} placeholder="general" />
        </Field>
        <Field label="Regreso máximo">
          <input type="time" value={f.maxReturn} onChange={(e) => change({ maxReturn: e.target.value })} />
        </Field>
        <Field label="Regreso máximo sábado">
          <input type="time" value={f.maxReturnSaturday} onChange={(e) => change({ maxReturnSaturday: e.target.value })} />
        </Field>
        <Checkbox wide checked={f.active} onChange={(active) => change({ active })}>
          Activa
        </Checkbox>
      </div>
    </Dialog>
  );
}

// ---------------- Zonas ----------------

export function Zonas() {
  const canEdit = useSession().can('operacion', 'edit');
  const zonas = useZonas();
  const [editing, setEditing] = useState<Zona | 'new' | null>(null);
  const [days, setDays] = useState<Zona | null>(null);
  const deactivate = useAction(
    (id: number) => unwrap(api.DELETE('/zonas/{id}', { params: { path: { id } } })),
    [keys.zonas],
    'Zona desactivada.',
  );

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Zonas</h2>
          <p className="muted">Las zonas EXT siguen los días de visita de su núcleo.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva zona
          </button>
        )}
      </div>
      <QueryState loading={zonas.isPending} error={zonas.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Zona</th>
                <th>Ruta</th>
                <th>Núcleo</th>
                <th>Días de visita</th>
                <th>Polígono</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {zonas.data?.map((z) => (
                <tr key={z.id} className={z.active ? '' : 'inactive'}>
                  <td>
                    <strong>{z.code}</strong>
                  </td>
                  <td>{z.rutaCode}</td>
                  <td>{z.zonaNucleoCode ?? <span className="muted">es núcleo</span>}</td>
                  <td>
                    <WeekdayBadges days={z.effectiveVisitDays} />
                    {z.zonaNucleoId && (
                      <div className="muted" style={{ fontSize: 12 }}>
                        heredados de {z.zonaNucleoCode}
                      </div>
                    )}
                  </td>
                  <td className="muted">{z.polygon?.length ? `${z.polygon.length} vértices` : 'sin dibujar'}</td>
                  <td>
                    <ActiveBadge active={z.active} />
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        {!z.zonaNucleoId && (
                          <button type="button" className="small" onClick={() => setDays(z)}>
                            Días
                          </button>
                        )}
                        <button type="button" className="small" onClick={() => setEditing(z)}>
                          Editar
                        </button>
                        {z.active && (
                          <button
                            type="button"
                            className="small danger"
                            disabled={deactivate.isPending}
                            onClick={() => confirm(`¿Desactivar la zona ${z.code}?`) && deactivate.mutate(z.id)}
                          >
                            Desactivar
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {zonas.data?.length === 0 && <div className="empty">Todavía no hay zonas.</div>}
        </div>
      </QueryState>
      {editing && <ZonaForm zona={editing === 'new' ? null : editing} zonas={zonas.data ?? []} onClose={() => setEditing(null)} />}
      {days && <VisitDaysForm zona={days} onClose={() => setDays(null)} />}
    </section>
  );
}

function WeekdayBadges({ days }: { days: Weekday[] }) {
  if (days.length === 0) return <span className="muted">sin días</span>;
  return (
    <span className="actions tight">
      {WEEKDAYS.filter((d) => days.includes(d.key)).map((d) => (
        <span key={d.key} className="badge brand" title={d.name}>
          {d.name.slice(0, 3)}
        </span>
      ))}
    </span>
  );
}

function ZonaForm({ zona, zonas, onClose }: { zona: Zona | null; zonas: Zona[]; onClose: () => void }) {
  const rutas = useRutas();
  const [code, setCode] = useState(zona?.code ?? '');
  const [rutaId, setRutaId] = useState(zona?.rutaId.toString() ?? '');
  const [nucleoId, setNucleoId] = useState(zona?.zonaNucleoId?.toString() ?? '');
  const [polygon, setPolygon] = useState(zona?.polygon?.map((v) => `${v.lat}, ${v.lng}`).join('\n') ?? '');
  const [active, setActive] = useState(zona?.active ?? true);
  const nucleos = zonas.filter((z) => !z.zonaNucleoId && z.id !== zona?.id && z.active);

  const save = useSave(
    () => {
      const body = {
        code: code.trim(),
        rutaId: Number(rutaId),
        zonaNucleoId: nucleoId ? Number(nucleoId) : null,
        polygon: parsePolygon(polygon),
        active,
      };
      return zona
        ? unwrap(api.PUT('/zonas/{id}', { params: { path: { id: zona.id } }, body }))
        : unwrap(api.POST('/zonas', { body }));
    },
    { refetch: [keys.zonas], message: zona ? 'Zona actualizada.' : 'Zona creada.', onDone: onClose },
  );

  return (
    <Dialog title={zona ? `Zona ${zona.code}` : 'Nueva zona'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Código" hint="Por ejemplo F1 o F1EXT.">
          <input autoFocus value={code} onChange={(e) => setCode(e.target.value)} required />
        </Field>
        <Field label="Ruta">
          <select value={rutaId} onChange={(e) => setRutaId(e.target.value)} required>
            <option value="">Elige…</option>
            {rutas.data?.map((r) => (
              <option key={r.id} value={r.id}>
                {r.code} · {r.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Núcleo" hint="Solo para zonas EXT.">
          <select value={nucleoId} onChange={(e) => setNucleoId(e.target.value)}>
            <option value="">Es núcleo</option>
            {nucleos.map((z) => (
              <option key={z.id} value={z.id}>
                {z.code}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Polígono" hint="Un vértice por renglón: latitud, longitud. El editor en mapa llega con Google Maps." wide>
          <textarea
            rows={5}
            value={polygon}
            onChange={(e) => setPolygon(e.target.value)}
            placeholder={'19.3550, -99.1900\n19.3550, -99.1800\n19.3450, -99.1800'}
          />
        </Field>
        <Checkbox wide checked={active} onChange={setActive}>
          Activa
        </Checkbox>
      </div>
    </Dialog>
  );
}

/** Only splits the text into numbers; the API validates and closes the polygon. */
function parsePolygon(text: string): Schemas['Vertex'][] | null {
  const lines = text.split('\n').map((l) => l.trim()).filter(Boolean);
  if (lines.length === 0) return null;
  return lines.map((l) => {
    const [lat, lng] = l.split(',').map((n) => Number(n.trim()));
    return { lat, lng };
  });
}

function VisitDaysForm({ zona, onClose }: { zona: Zona; onClose: () => void }) {
  const [days, setDays] = useState<Weekday[]>(zona.visitDays);
  const save = useSave(
    () => unwrap(api.PUT('/zonas/{id}/dias', { params: { path: { id: zona.id } }, body: { days } })),
    { refetch: [keys.zonas], message: 'Días de visita actualizados.', onDone: onClose },
  );
  const toggle = (d: Weekday) => setDays((list) => (list.includes(d) ? list.filter((x) => x !== d) : [...list, d]));

  return (
    <Dialog title={`Días de visita · ${zona.code}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <p className="muted">Sus zonas EXT toman estos mismos días.</p>
      <div className="actions">
        {WEEKDAYS.map((d) => (
          <span key={d.key} style={{ minWidth: 110 }}>
            <Checkbox checked={days.includes(d.key)} onChange={() => toggle(d.key)}>
              {d.name}
            </Checkbox>
          </span>
        ))}
      </div>
    </Dialog>
  );
}

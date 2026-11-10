import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys, useZonas } from '../../api/queries';
import {
  ActiveBadge,
  Checkbox,
  Dialog,
  Field,
  QueryState,
  formatDate,
  optional,
  optionalNumber,
  useAction,
  useSave,
} from '../../components/Common';
import { useSession } from '../../session/Session';

const useCanEdit = () => useSession().can('operacion', 'edit');

// ---------------- Tiendas y bodegas ----------------

type Lugar = Schemas['LugarDto'];
const LUGAR_TYPES: Record<Lugar['type'], string> = { Base: 'Base', Bodega: 'Bodega / insumos', Tienda: 'Tienda 7clean' };

export function Lugares() {
  const canEdit = useCanEdit();
  const lugares = useQuery({
    queryKey: keys.lugares,
    queryFn: () => unwrap(api.GET('/lugares', { params: { query: { includeInactive: true } } })),
  });
  const [editing, setEditing] = useState<Lugar | 'new' | null>(null);
  const deactivate = useAction(
    (id: number) => unwrap(api.DELETE('/lugares/{id}', { params: { path: { id } } })),
    [keys.lugares],
    'Lugar desactivado.',
  );

  return (
    <section className="card">
      <div className="card__header">
        <h2>Tiendas y bodegas</h2>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nuevo lugar
          </button>
        )}
      </div>
      <QueryState loading={lugares.isPending} error={lugares.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Tipo</th>
                <th>Dirección</th>
                <th>Coordenadas</th>
                <th>Horario</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {lugares.data?.map((l) => (
                <tr key={l.id} className={l.active ? '' : 'inactive'}>
                  <td>
                    <strong>{l.name}</strong>
                  </td>
                  <td>{LUGAR_TYPES[l.type]}</td>
                  <td>{l.address}</td>
                  <td className="num muted">{l.lat != null && l.lng != null ? `${l.lat}, ${l.lng}` : '—'}</td>
                  <td className="muted">{l.openingHours ?? '—'}</td>
                  <td>
                    <ActiveBadge active={l.active} />
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        <button type="button" className="small" onClick={() => setEditing(l)}>
                          Editar
                        </button>
                        {l.active && (
                          <button
                            type="button"
                            className="small danger"
                            disabled={deactivate.isPending}
                            onClick={() => confirm(`¿Desactivar ${l.name}?`) && deactivate.mutate(l.id)}
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
        </div>
      </QueryState>
      {editing && <LugarForm lugar={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function LugarForm({ lugar, onClose }: { lugar: Lugar | null; onClose: () => void }) {
  const [f, setF] = useState({
    name: lugar?.name ?? '',
    type: lugar?.type ?? ('Tienda' as Lugar['type']),
    address: lugar?.address ?? '',
    lat: lugar?.lat?.toString() ?? '',
    lng: lugar?.lng?.toString() ?? '',
    openingHours: lugar?.openingHours ?? '',
    active: lugar?.active ?? true,
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const save = useSave(
    () => {
      const body = {
        name: f.name.trim(),
        type: f.type,
        address: f.address.trim(),
        lat: optionalNumber(f.lat),
        lng: optionalNumber(f.lng),
        openingHours: optional(f.openingHours),
        active: f.active,
      };
      return lugar
        ? unwrap(api.PUT('/lugares/{id}', { params: { path: { id: lugar.id } }, body }))
        : unwrap(api.POST('/lugares', { body }));
    },
    { refetch: [keys.lugares], message: lugar ? 'Lugar actualizado.' : 'Lugar creado.', onDone: onClose },
  );

  return (
    <Dialog title={lugar?.name ?? 'Nuevo lugar'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Nombre">
          <input autoFocus value={f.name} onChange={(e) => change({ name: e.target.value })} required />
        </Field>
        <Field label="Tipo">
          <select value={f.type} onChange={(e) => change({ type: e.target.value as Lugar['type'] })}>
            {Object.entries(LUGAR_TYPES).map(([key, name]) => (
              <option key={key} value={key}>
                {name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Dirección" wide>
          <input value={f.address} onChange={(e) => change({ address: e.target.value })} required />
        </Field>
        <Field label="Latitud">
          <input inputMode="decimal" value={f.lat} onChange={(e) => change({ lat: e.target.value })} />
        </Field>
        <Field label="Longitud">
          <input inputMode="decimal" value={f.lng} onChange={(e) => change({ lng: e.target.value })} />
        </Field>
        <Field label="Horario" wide>
          <input value={f.openingHours} onChange={(e) => change({ openingHours: e.target.value })} placeholder="L a V 9:00 a 19:00" />
        </Field>
        <Checkbox wide checked={f.active} onChange={(active) => change({ active })}>
          Activo
        </Checkbox>
      </div>
    </Dialog>
  );
}

// ---------------- Días inhábiles ----------------

export function NonWorkingDays() {
  const canEdit = useCanEdit();
  const days = useQuery({ queryKey: keys.nonWorkingDays, queryFn: () => unwrap(api.GET('/dias-inhabiles')) });
  const [adding, setAdding] = useState(false);
  const remove = useAction(
    (id: number) => unwrap(api.DELETE('/dias-inhabiles/{id}', { params: { path: { id } } })),
    [keys.nonWorkingDays],
    'Día inhábil quitado.',
  );

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Días inhábiles</h2>
          <p className="muted">No se agenda ni se rutea en estas fechas.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setAdding(true)}>
            Agregar día
          </button>
        )}
      </div>
      <QueryState loading={days.isPending} error={days.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Motivo</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {days.data?.map((d) => (
                <tr key={d.id}>
                  <td>
                    <strong>{formatDate(d.date)}</strong>
                  </td>
                  <td>{d.reason}</td>
                  <td className="end">
                    {canEdit && (
                      <button
                        type="button"
                        className="small danger"
                        disabled={remove.isPending}
                        onClick={() => confirm(`¿Quitar ${formatDate(d.date)}?`) && remove.mutate(d.id)}
                      >
                        Quitar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {days.data?.length === 0 && <div className="empty">No hay días inhábiles registrados.</div>}
        </div>
      </QueryState>
      {adding && <NonWorkingDayForm onClose={() => setAdding(false)} />}
    </section>
  );
}

function NonWorkingDayForm({ onClose }: { onClose: () => void }) {
  const [date, setDate] = useState('');
  const [reason, setReason] = useState('');
  const save = useSave(() => unwrap(api.POST('/dias-inhabiles', { body: { date, reason: reason.trim() } })), {
    refetch: [keys.nonWorkingDays],
    message: 'Día inhábil agregado.',
    onDone: onClose,
  });
  return (
    <Dialog title="Agregar día inhábil" onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <Field label="Fecha">
        <input type="date" autoFocus value={date} onChange={(e) => setDate(e.target.value)} required />
      </Field>
      <Field label="Motivo">
        <input value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Día de muertos" required />
      </Field>
    </Dialog>
  );
}

// ---------------- Cobertura ----------------

type Coverage = Schemas['CoverageDto'];

export function CoveragePage() {
  const canEdit = useCanEdit();
  const zonas = useZonas();
  const [search, setSearch] = useState('');
  const [zonaId, setZonaId] = useState('');
  const coverage = useQuery({
    queryKey: [...keys.coverage, search, zonaId],
    queryFn: () =>
      unwrap(
        api.GET('/cobertura', {
          params: { query: { search: optional(search) ?? undefined, zonaId: zonaId ? Number(zonaId) : undefined } },
        }),
      ),
    placeholderData: (previous) => previous,
  });
  const [editing, setEditing] = useState<Coverage | 'new' | null>(null);
  const remove = useAction(
    (id: number) => unwrap(api.DELETE('/cobertura/{id}', { params: { path: { id } } })),
    [keys.coverage],
    'Registro quitado.',
  );

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Cobertura</h2>
          <p className="muted">Código postal y colonia que atiende cada zona.</p>
        </div>
        <div className="actions">
          <input type="search" placeholder="CP, colonia o alcaldía" value={search} onChange={(e) => setSearch(e.target.value)} />
          <select value={zonaId} onChange={(e) => setZonaId(e.target.value)} aria-label="Zona">
            <option value="">Todas las zonas</option>
            {zonas.data?.map((z) => (
              <option key={z.id} value={z.id}>
                {z.code}
              </option>
            ))}
          </select>
          {canEdit && (
            <button type="button" className="primary" onClick={() => setEditing('new')}>
              Agregar
            </button>
          )}
        </div>
      </div>
      <QueryState loading={coverage.isPending} error={coverage.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>CP</th>
                <th>Colonia</th>
                <th>Alcaldía</th>
                <th>Zona</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {coverage.data?.map((c) => (
                <tr key={c.id}>
                  <td className="num">
                    <strong>{c.postalCode}</strong>
                  </td>
                  <td>{c.neighborhood}</td>
                  <td>{c.borough}</td>
                  <td>
                    <span className="badge brand">{c.zonaCode}</span>
                  </td>
                  <td>
                    {canEdit && (
                      <div className="actions end">
                        <button type="button" className="small" onClick={() => setEditing(c)}>
                          Editar
                        </button>
                        <button
                          type="button"
                          className="small danger"
                          disabled={remove.isPending}
                          onClick={() => confirm(`¿Quitar ${c.postalCode} ${c.neighborhood}?`) && remove.mutate(c.id)}
                        >
                          Quitar
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {coverage.data?.length === 0 && <div className="empty">Sin resultados.</div>}
        </div>
      </QueryState>
      {editing && <CoverageForm coverage={editing === 'new' ? null : editing} zonas={zonas.data ?? []} onClose={() => setEditing(null)} />}
    </section>
  );
}

function CoverageForm({ coverage, zonas, onClose }: { coverage: Coverage | null; zonas: Schemas['ZonaDto'][]; onClose: () => void }) {
  const [f, setF] = useState({
    postalCode: coverage?.postalCode ?? '',
    neighborhood: coverage?.neighborhood ?? '',
    borough: coverage?.borough ?? '',
    zonaId: coverage?.zonaId.toString() ?? '',
  });
  const change = (c: Partial<typeof f>) => setF((v) => ({ ...v, ...c }));
  const save = useSave(
    () => {
      const body = {
        postalCode: f.postalCode.trim(),
        neighborhood: f.neighborhood.trim(),
        borough: f.borough.trim(),
        zonaId: Number(f.zonaId),
      };
      return coverage
        ? unwrap(api.PUT('/cobertura/{id}', { params: { path: { id: coverage.id } }, body }))
        : unwrap(api.POST('/cobertura', { body }));
    },
    { refetch: [keys.coverage], message: 'Cobertura guardada.', onDone: onClose },
  );
  return (
    <Dialog
      title={coverage ? 'Editar cobertura' : 'Agregar cobertura'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Código postal">
          <input
            autoFocus
            inputMode="numeric"
            maxLength={5}
            value={f.postalCode}
            onChange={(e) => change({ postalCode: e.target.value })}
            required
          />
        </Field>
        <Field label="Zona">
          <select value={f.zonaId} onChange={(e) => change({ zonaId: e.target.value })} required>
            <option value="">Elige…</option>
            {zonas
              .filter((z) => z.active)
              .map((z) => (
                <option key={z.id} value={z.id}>
                  {z.code}
                </option>
              ))}
          </select>
        </Field>
        <Field label="Colonia" wide>
          <input value={f.neighborhood} onChange={(e) => change({ neighborhood: e.target.value })} required />
        </Field>
        <Field label="Alcaldía" wide>
          <input value={f.borough} onChange={(e) => change({ borough: e.target.value })} required />
        </Field>
      </div>
    </Dialog>
  );
}

// ---------------- Otros catálogos ----------------

type Reason = Schemas['ReasonDto'];
const REASON_TYPES: Record<Reason['type'], string> = {
  FailedEntrega: 'No entrega',
  PlantaDelay: 'Atraso en planta',
  ContactResult: 'Resultado de contacto',
};

export function OtherCatalogs() {
  return (
    <div className="stack">
      <Reasons />
      <ParadaTypes />
      <LineasServicio />
    </div>
  );
}

function Reasons() {
  const canEdit = useCanEdit();
  const reasons = useQuery({ queryKey: keys.reasons, queryFn: () => unwrap(api.GET('/motivos')) });
  const [editing, setEditing] = useState<Reason | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <h2>Motivos</h2>
        {canEdit && (
          <button type="button" onClick={() => setEditing('new')}>
            Nuevo motivo
          </button>
        )}
      </div>
      <QueryState loading={reasons.isPending} error={reasons.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Texto</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {reasons.data?.map((m) => (
                <tr key={m.id} className={m.active ? '' : 'inactive'}>
                  <td>{REASON_TYPES[m.type]}</td>
                  <td>{m.text}</td>
                  <td>
                    <ActiveBadge active={m.active} />
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(m)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {reasons.data?.length === 0 && <div className="empty">Sin motivos.</div>}
        </div>
      </QueryState>
      {editing && <ReasonForm reason={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function ReasonForm({ reason, onClose }: { reason: Reason | null; onClose: () => void }) {
  const [type, setType] = useState<Reason['type']>(reason?.type ?? 'FailedEntrega');
  const [text, setText] = useState(reason?.text ?? '');
  const [active, setActive] = useState(reason?.active ?? true);
  const save = useSave(
    () => {
      const body = { type, text: text.trim(), active };
      return reason
        ? unwrap(api.PUT('/motivos/{id}', { params: { path: { id: reason.id } }, body }))
        : unwrap(api.POST('/motivos', { body }));
    },
    { refetch: [keys.reasons], message: 'Motivo guardado.', onDone: onClose },
  );
  return (
    <Dialog title={reason ? 'Editar motivo' : 'Nuevo motivo'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <Field label="Tipo">
        <select value={type} onChange={(e) => setType(e.target.value as Reason['type'])}>
          {Object.entries(REASON_TYPES).map(([key, name]) => (
            <option key={key} value={key}>
              {name}
            </option>
          ))}
        </select>
      </Field>
      <Field label="Texto">
        <input autoFocus value={text} onChange={(e) => setText(e.target.value)} required />
      </Field>
      <Checkbox checked={active} onChange={setActive}>
        Activo
      </Checkbox>
    </Dialog>
  );
}

type ParadaType = Schemas['ParadaTypeDto'];

function ParadaTypes() {
  const canEdit = useCanEdit();
  const types = useQuery({ queryKey: keys.paradaTypes, queryFn: () => unwrap(api.GET('/tipos-parada')) });
  const [editing, setEditing] = useState<ParadaType | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <h2>Tipos de parada</h2>
        {canEdit && (
          <button type="button" onClick={() => setEditing('new')}>
            Nuevo tipo
          </button>
        )}
      </div>
      <QueryState loading={types.isPending} error={types.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Campos requeridos</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {types.data?.map((t) => (
                <tr key={t.id} className={t.active ? '' : 'inactive'}>
                  <td>
                    <strong>{t.name}</strong>
                  </td>
                  <td className="muted">{t.requiredFields ?? '—'}</td>
                  <td>
                    <ActiveBadge active={t.active} />
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(t)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
      {editing && <ParadaTypeForm type={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function ParadaTypeForm({ type, onClose }: { type: ParadaType | null; onClose: () => void }) {
  const [name, setName] = useState(type?.name ?? '');
  const [fields, setFields] = useState(type?.requiredFields ?? '');
  const [active, setActive] = useState(type?.active ?? true);
  const save = useSave(
    () => {
      const body = { name: name.trim(), requiredFields: optional(fields), active };
      return type
        ? unwrap(api.PUT('/tipos-parada/{id}', { params: { path: { id: type.id } }, body }))
        : unwrap(api.POST('/tipos-parada', { body }));
    },
    { refetch: [keys.paradaTypes], message: 'Tipo de parada guardado.', onDone: onClose },
  );
  return (
    <Dialog
      title={type ? `Editar ${type.name}` : 'Nuevo tipo de parada'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Nombre">
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>
      <Field label="Campos requeridos" hint="Opcional.">
        <input value={fields} onChange={(e) => setFields(e.target.value)} />
      </Field>
      <Checkbox checked={active} onChange={setActive}>
        Activo
      </Checkbox>
    </Dialog>
  );
}

type LineaServicio = Schemas['LineaServicioDto'];

function LineasServicio() {
  const canEdit = useCanEdit();
  const lineas = useQuery({ queryKey: keys.lineasServicio, queryFn: () => unwrap(api.GET('/lineas-servicio')) });
  const [editing, setEditing] = useState<LineaServicio | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Líneas de servicio</h2>
          <p className="muted">Días de proceso vacío = usa la regla general.</p>
        </div>
        {canEdit && (
          <button type="button" onClick={() => setEditing('new')}>
            Nueva línea
          </button>
        )}
      </div>
      <QueryState loading={lineas.isPending} error={lineas.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Días de proceso</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {lineas.data?.map((l) => (
                <tr key={l.id} className={l.active ? '' : 'inactive'}>
                  <td>
                    <strong>{l.name}</strong>
                  </td>
                  <td className="num">{l.processingDays ?? <span className="muted">general</span>}</td>
                  <td>
                    <ActiveBadge active={l.active} />
                  </td>
                  <td className="end">
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(l)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
      {editing && <LineaServicioForm linea={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

function LineaServicioForm({ linea, onClose }: { linea: LineaServicio | null; onClose: () => void }) {
  const [name, setName] = useState(linea?.name ?? '');
  const [days, setDays] = useState(linea?.processingDays?.toString() ?? '');
  const [active, setActive] = useState(linea?.active ?? true);
  const save = useSave(
    () => {
      const body = { name: name.trim(), processingDays: optionalNumber(days), active };
      return linea
        ? unwrap(api.PUT('/lineas-servicio/{id}', { params: { path: { id: linea.id } }, body }))
        : unwrap(api.POST('/lineas-servicio', { body }));
    },
    { refetch: [keys.lineasServicio], message: 'Línea guardada.', onDone: onClose },
  );
  return (
    <Dialog
      title={linea ? `Editar ${linea.name}` : 'Nueva línea de servicio'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <Field label="Nombre">
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required />
      </Field>
      <Field label="Días de proceso">
        <input type="number" min={0} value={days} onChange={(e) => setDays(e.target.value)} placeholder="general" />
      </Field>
      <Checkbox checked={active} onChange={setActive}>
        Activa
      </Checkbox>
    </Dialog>
  );
}

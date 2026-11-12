import { useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys } from '../../api/queries';
import { useSession } from '../../session/Session';
import { useNotify } from '../../components/Toasts';
import { Checkbox, Dialog, Field, PageHeader, QueryState, formatDate, optional, optionalNumber, useAction, useSave } from '../../components/Common';

type Invitado = Schemas['InvitadoDto'];
type Direccion = Schemas['DireccionDto'];
type Turno = Schemas['Turno'];

const TURNOS: { key: Turno; name: string }[] = [
  { key: 'CualquierHora', name: 'Cualquier hora' },
  { key: 'Manana', name: 'Mañana' },
  { key: 'Tarde', name: 'Tarde' },
  { key: 'HoraEspecifica', name: 'Hora específica' },
];
const turnoName = (t: Turno) => TURNOS.find((x) => x.key === t)?.name ?? t;

const SOURCES: Record<Schemas['ZonaSource'], string> = {
  Poligono: 'por ubicación',
  CodigoPostal: 'por código postal',
  Manual: 'asignada a mano',
  FueraDeCobertura: 'fuera de cobertura',
};

/** Deleting is a baja lógica, fixed to SuperUser and Administración in the API. */
function useIsAdministrator() {
  const role = useSession().user?.role;
  return role === 'SuperUser' || role === 'Administracion';
}

/** "2026-11-12T10:00" typed in Mexico City time (UTC-6 all year since 2022) as an ISO instant. */
function mexicoCityInstant(local: string) {
  return local ? `${local}:00-06:00` : null;
}

function addressLine(d: Direccion) {
  const number = [d.exteriorNumber, d.interiorNumber && `int. ${d.interiorNumber}`].filter(Boolean).join(' ');
  return [`${d.street} ${number}`.trim(), d.neighborhood, `CP ${d.postalCode}`].filter(Boolean).join(', ');
}

export function Agendar() {
  const canEdit = useSession().can('agendar', 'edit');
  const [invitadoId, setInvitadoId] = useState<number | null>(null);
  const [direccionId, setDireccionId] = useState<number | null>(null);
  const [capturing, setCapturing] = useState(false);

  const invitado = useQuery({
    queryKey: ['invitados', invitadoId],
    queryFn: () => unwrap(api.GET('/invitados/{id}', { params: { path: { id: invitadoId! } } })),
    enabled: invitadoId !== null,
  });

  // Select the first address when the invitado changes or the chosen one disappears.
  useEffect(() => {
    const list = invitado.data?.direcciones ?? [];
    if (!list.some((d) => d.id === direccionId)) setDireccionId(list[0]?.id ?? null);
  }, [invitado.data, direccionId]);

  function choose(id: number | null) {
    setInvitadoId(id);
    setDireccionId(null);
    setCapturing(false);
  }

  return (
    <div className="stack">
      <PageHeader title="Agendar" description="Captura de una recolección por teléfono o WhatsApp. La fecha la calcula el motor de agenda del API." />
      <div className="agendar">
        <div className="stack">
          <InvitadoSearch onChoose={choose} onNew={() => { choose(null); setCapturing(true); }} canEdit={canEdit} />
          {capturing && <InvitadoForm onClose={() => setCapturing(false)} onSaved={(i) => choose(i.id)} />}
          {invitadoId !== null && (
            <QueryState loading={invitado.isPending} error={invitado.error}>
              {invitado.data && (
                <InvitadoCard
                  invitado={invitado.data}
                  direccionId={direccionId}
                  onDireccion={setDireccionId}
                  onRemoved={() => choose(null)}
                  canEdit={canEdit}
                />
              )}
            </QueryState>
          )}
        </div>
        {invitado.data && direccionId !== null ? (
          <Schedule invitado={invitado.data} direccionId={direccionId} canEdit={canEdit} />
        ) : (
          <section className="card">
            <div className="empty">Busca o captura al invitado para ver cómo se agenda.</div>
          </section>
        )}
      </div>
      <PedidoList />
    </div>
  );
}

// ---- Invitado ----

function InvitadoSearch({ onChoose, onNew, canEdit }: { onChoose: (id: number) => void; onNew: () => void; canEdit: boolean }) {
  const [text, setText] = useState('');
  const [q, setQ] = useState('');
  useEffect(() => {
    const t = setTimeout(() => setQ(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);

  const results = useQuery({
    queryKey: ['invitados', 'buscar', q],
    queryFn: () => unwrap(api.GET('/invitados/buscar', { params: { query: { q } } })),
    enabled: q.length >= 2,
  });

  return (
    <section className="card">
      <div className="card__header">
        <h2>Invitado</h2>
        {canEdit && (
          <button type="button" onClick={onNew}>
            Nuevo invitado
          </button>
        )}
      </div>
      <div className="card__body stack">
        <Field label="Buscar por nombre o teléfono">
          <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Ana López o 55 1234 5678" autoFocus />
        </Field>
        {q.length >= 2 && (
          <QueryState loading={results.isPending} error={results.error}>
            {results.data?.length === 0 ? (
              <div className="muted">Sin resultados. {canEdit && 'Captúralo con "Nuevo invitado".'}</div>
            ) : (
              <ul className="pick-list">
                {results.data?.map((i) => (
                  <li key={i.id}>
                    <button type="button" onClick={() => { onChoose(i.id); setText(''); }}>
                      <strong>{i.name}</strong>
                      <span className="muted">
                        {i.whatsAppPhone} · {i.direcciones.length} dirección{i.direcciones.length === 1 ? '' : 'es'}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </QueryState>
        )}
      </div>
    </section>
  );
}

function InvitadoCard({
  invitado,
  direccionId,
  onDireccion,
  onRemoved,
  canEdit,
}: {
  invitado: Invitado;
  direccionId: number | null;
  onDireccion: (id: number) => void;
  onRemoved: () => void;
  canEdit: boolean;
}) {
  const isAdmin = useIsAdministrator();
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState(false);
  const queryClient = useQueryClient();
  const notify = useNotify();
  const [removing, setRemoving] = useState(false);

  async function remove() {
    const pedidos = await unwrap(api.GET('/pedidos', { params: { query: { invitadoId: invitado.id } } }));
    const extra = pedidos.length ? ` y sus ${pedidos.length} pedido${pedidos.length === 1 ? '' : 's'}` : '';
    if (!confirm(`¿Eliminar a ${invitado.name}${extra}? Se dan de baja; sus lugares en la agenda quedan libres.`)) return;
    setRemoving(true);
    try {
      await unwrap(api.DELETE('/invitados/{id}', { params: { path: { id: invitado.id } } }));
      await queryClient.invalidateQueries({ queryKey: ['invitados'] });
      await queryClient.invalidateQueries({ queryKey: keys.pedidos });
      await queryClient.invalidateQueries({ queryKey: ['agenda'] });
      notify(`${invitado.name} eliminado.`);
      onRemoved();
    } catch (e) {
      notify((e as Error).message, true);
    } finally {
      setRemoving(false);
    }
  }

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>{invitado.name}</h2>
          <p className="muted">
            WhatsApp {invitado.whatsAppPhone}
            {invitado.howFoundUs && ` · ${invitado.howFoundUs}`}
          </p>
        </div>
        <div className="actions tight">
          {canEdit && (
            <button type="button" className="small" onClick={() => setEditing(true)}>
              Editar
            </button>
          )}
          {isAdmin && (
            <button type="button" className="small danger" disabled={removing} onClick={() => void remove()}>
              Eliminar
            </button>
          )}
        </div>
      </div>
      <div className="card__body stack">
        {invitado.direcciones.length === 0 && <div className="alert">No tiene direcciones. Agrega una para agendar.</div>}
        {invitado.direcciones.map((d) => (
          <DireccionOption key={d.id} invitadoId={invitado.id} direccion={d} selected={d.id === direccionId} onSelect={() => onDireccion(d.id)} canEdit={canEdit} />
        ))}
        {canEdit && (
          <div>
            <button type="button" className="small" onClick={() => setAdding(true)}>
              Agregar dirección
            </button>
          </div>
        )}
      </div>
      {adding && <DireccionForm invitadoId={invitado.id} onClose={() => setAdding(false)} />}
      {editing && <InvitadoForm invitado={invitado} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
    </section>
  );
}

function DireccionOption({
  invitadoId,
  direccion: d,
  selected,
  onSelect,
  canEdit,
}: {
  invitadoId: number;
  direccion: Direccion;
  selected: boolean;
  onSelect: () => void;
  canEdit: boolean;
}) {
  const zonas = useQuery({ queryKey: keys.zonas, queryFn: () => unwrap(api.GET('/zonas')), enabled: !d.zonaId && canEdit });
  const assign = useSave(
    (zonaId: number) =>
      unwrap(
        api.PUT('/invitados/{id}/direcciones/{direccionId}', {
          params: { path: { id: invitadoId, direccionId: d.id } },
          body: { ...d, zonaId },
        }),
      ),
    { refetch: [['invitados'], ['agenda']], message: 'Zona asignada.' },
  );

  return (
    <div className={`address-option${selected ? ' selected' : ''}`}>
      <label>
        <input type="radio" checked={selected} onChange={onSelect} />
        <span>
          {d.label && <strong>{d.label}: </strong>}
          {addressLine(d)}
        </span>
      </label>
      {d.zonaCode ? (
        <span className="badge brand">
          Zona {d.zonaCode}
          {d.zonaAssignedManually && ' (a mano)'}
        </span>
      ) : (
        <div className="actions tight">
          <span className="badge warning">Fuera de cobertura</span>
          {canEdit && (
            <select value="" onChange={(e) => e.target.value && assign.mutate(Number(e.target.value))} disabled={assign.isPending}>
              <option value="">Elegir zona…</option>
              {zonas.data?.map((z) => (
                <option key={z.id} value={z.id}>
                  {z.code} ({z.rutaCode})
                </option>
              ))}
            </select>
          )}
        </div>
      )}
      {assign.error && <div className="alert error">{assign.error.message}</div>}
    </div>
  );
}

type DireccionFields = {
  label: string;
  street: string;
  exteriorNumber: string;
  interiorNumber: string;
  postalCode: string;
  neighborhood: string;
  borough: string;
  references: string;
  coordinates: string;
};
const emptyDireccion: DireccionFields = {
  label: '', street: '', exteriorNumber: '', interiorNumber: '', postalCode: '', neighborhood: '', borough: '', references: '', coordinates: '',
};

/** "19.3608, -99.1968" (copied from Google Maps) to latitude and longitude. Autocomplete arrives with the Google keys. */
function toDireccionRequest(f: DireccionFields): Schemas['SaveDireccionRequest'] {
  const [lat, lng] = f.coordinates.split(',').map((x) => Number(x.trim()));
  const hasPoint = f.coordinates.trim() !== '' && Number.isFinite(lat) && Number.isFinite(lng);
  return {
    label: optional(f.label),
    street: f.street.trim(),
    exteriorNumber: optional(f.exteriorNumber),
    interiorNumber: optional(f.interiorNumber),
    postalCode: f.postalCode.trim(),
    neighborhood: optional(f.neighborhood),
    borough: optional(f.borough),
    references: optional(f.references),
    lat: hasPoint ? lat : null,
    lng: hasPoint ? lng : null,
    zonaId: null,
  };
}

function DireccionFieldsForm({ value, onChange }: { value: DireccionFields; onChange: (v: DireccionFields) => void }) {
  const set = (k: keyof DireccionFields) => (e: { target: { value: string } }) => onChange({ ...value, [k]: e.target.value });
  return (
    <div className="form-grid">
      <Field label="Calle" wide>
        <input value={value.street} onChange={set('street')} required />
      </Field>
      <Field label="Número">
        <input value={value.exteriorNumber} onChange={set('exteriorNumber')} />
      </Field>
      <Field label="Interior">
        <input value={value.interiorNumber} onChange={set('interiorNumber')} />
      </Field>
      <Field label="Código postal">
        <input value={value.postalCode} onChange={set('postalCode')} inputMode="numeric" maxLength={5} required />
      </Field>
      <Field label="Colonia">
        <input value={value.neighborhood} onChange={set('neighborhood')} />
      </Field>
      <Field label="Alcaldía">
        <input value={value.borough} onChange={set('borough')} />
      </Field>
      <Field label="Etiqueta">
        <input value={value.label} onChange={set('label')} placeholder="Casa" />
      </Field>
      <Field label="Ubicación (latitud, longitud)" hint="Cópiala de Google Maps. Decide la zona; sin ella se usa el código postal." wide>
        <input value={value.coordinates} onChange={set('coordinates')} placeholder="19.3608, -99.1968" />
      </Field>
      <Field label="Referencias" wide>
        <input value={value.references} onChange={set('references')} />
      </Field>
    </div>
  );
}

function DireccionForm({ invitadoId, onClose }: { invitadoId: number; onClose: () => void }) {
  const [fields, setFields] = useState(emptyDireccion);
  const save = useSave(
    () => unwrap(api.POST('/invitados/{id}/direcciones', { params: { path: { id: invitadoId } }, body: toDireccionRequest(fields) })),
    { refetch: [['invitados']], message: 'Dirección agregada.', onDone: onClose },
  );
  return (
    <Dialog title="Agregar dirección" onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <DireccionFieldsForm value={fields} onChange={setFields} />
    </Dialog>
  );
}

function InvitadoForm({ invitado, onClose, onSaved }: { invitado?: Invitado; onClose: () => void; onSaved: (i: Invitado) => void }) {
  const [name, setName] = useState(invitado?.name ?? '');
  const [phone, setPhone] = useState(invitado?.whatsAppPhone ?? '');
  const [otherPhone, setOtherPhone] = useState(invitado?.otherPhone ?? '');
  const [email, setEmail] = useState(invitado?.email ?? '');
  const [howFoundUs, setHowFoundUs] = useState(invitado?.howFoundUs ?? '');
  const [campaign, setCampaign] = useState(invitado?.campaign ?? '');
  const [notes, setNotes] = useState(invitado?.notes ?? '');
  const [direccion, setDireccion] = useState(emptyDireccion);

  const save = useSave(
    () => {
      const body: Schemas['SaveInvitadoRequest'] = {
        name: name.trim(),
        whatsAppPhone: phone,
        otherPhone: optional(otherPhone),
        email: optional(email),
        type: invitado?.type ?? 'ClienteFinal',
        howFoundUs: optional(howFoundUs),
        campaign: optional(campaign),
        notes: optional(notes),
        direccion: invitado ? null : toDireccionRequest(direccion),
      };
      return invitado
        ? unwrap(api.PUT('/invitados/{id}', { params: { path: { id: invitado.id } }, body }))
        : unwrap(api.POST('/invitados', { body }));
    },
    { refetch: [['invitados']], message: invitado ? 'Invitado actualizado.' : 'Invitado capturado.', onDone: onSaved },
  );

  return (
    <Dialog
      title={invitado ? `Editar a ${invitado.name}` : 'Nuevo invitado'}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Nombre" wide>
          <input value={name} onChange={(e) => setName(e.target.value)} required autoFocus />
        </Field>
        <Field label="WhatsApp" hint="10 dígitos">
          <input value={phone} onChange={(e) => setPhone(e.target.value)} inputMode="tel" required />
        </Field>
        <Field label="Otro teléfono">
          <input value={otherPhone} onChange={(e) => setOtherPhone(e.target.value)} inputMode="tel" />
        </Field>
        <Field label="Correo">
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </Field>
        <Field label="Cómo nos conoció">
          <input value={howFoundUs} onChange={(e) => setHowFoundUs(e.target.value)} />
        </Field>
        <Field label="Campaña">
          <input value={campaign} onChange={(e) => setCampaign(e.target.value)} />
        </Field>
        <Field label="Observaciones" wide>
          <input value={notes} onChange={(e) => setNotes(e.target.value)} />
        </Field>
      </div>
      {!invitado && (
        <>
          <h3>Dirección</h3>
          <DireccionFieldsForm value={direccion} onChange={setDireccion} />
        </>
      )}
    </Dialog>
  );
}

// ---- Schedule ----

function Schedule({ invitado, direccionId, canEdit }: { invitado: Invitado; direccionId: number; canEdit: boolean }) {
  const [turno, setTurno] = useState<Turno>('CualquierHora');
  const [specificTime, setSpecificTime] = useState('');
  const [garments, setGarments] = useState('');
  const [lineas, setLineas] = useState<number[]>([]);
  const [notes, setNotes] = useState('');
  const [priority, setPriority] = useState(false);
  const [registeredAt, setRegisteredAt] = useState('');
  const [date, setDate] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const lineasServicio = useQuery({ queryKey: keys.lineasServicio, queryFn: () => unwrap(api.GET('/lineas-servicio')) });
  const registered = mexicoCityInstant(registeredAt);
  const availability = useQuery({
    queryKey: ['agenda', direccionId, registered],
    queryFn: () =>
      unwrap(
        api.POST('/agenda/disponibilidad', {
          body: { direccionId, lat: null, lng: null, postalCode: null, neighborhood: null, zonaId: null, registeredAt: registered },
        }),
      ),
  });
  const a = availability.data;

  // A new address or hora de registro starts again from the date the engine proposes.
  useEffect(() => {
    setDate(null);
    setNotice(null);
  }, [direccionId, registered]);
  const chosen = date ?? a?.date ?? null;

  const schedule = useSave(
    () =>
      unwrap(
        api.POST('/agenda/agendar', {
          body: {
            invitadoId: invitado.id,
            direccionId,
            turno,
            specificTime: turno === 'HoraEspecifica' ? `${specificTime}:00` : null,
            priority,
            garmentsApprox: optionalNumber(garments),
            driverNotes: optional(notes),
            lineaServicioIds: lineas,
            registeredAt: registered,
            date: date,
          },
        }),
      ),
    {
      refetch: [['agenda'], keys.pedidos],
      onDone: (r) => {
        setNotice(r.message);
        setDate(null);
        if (r.scheduled) {
          setGarments('');
          setNotes('');
          setPriority(false);
        }
      },
    },
  );

  return (
    <section className="card agendar__panel">
      <div className="card__header">
        <h2>Así se agenda</h2>
        {a?.zonaCode && (
          <span className="badge brand">
            Zona {a.zonaCode} · Ruta {a.rutaCode} · {SOURCES[a.zonaSource]}
          </span>
        )}
      </div>
      <div className="card__body stack">
        <QueryState loading={availability.isPending} error={availability.error}>
          {a?.outOfCoverage ? (
            <div className="alert">{a.explanation[0]} Asígnala en la dirección del invitado.</div>
          ) : (
            a && (
              <>
                {chosen ? (
                  <div className="agendar__date">
                    <span className="muted">Fecha de recolección</span>
                    <strong>{formatDate(chosen)}</strong>
                    {!date && a.place && (
                      <span>
                        Lugar {a.place} de {a.cupo}
                      </span>
                    )}
                  </div>
                ) : (
                  <div className="alert">No hay días con cupo en los próximos días.</div>
                )}
                <ol className="agendar__steps">
                  {a.explanation.map((line, i) => (
                    <li key={i}>{line}</li>
                  ))}
                </ol>
                {a.days.length > 0 && (
                  <div>
                    <span className="muted">Otros días de la zona</span>
                    <div className="day-chips">
                      {a.days.map((d) => (
                        <button
                          type="button"
                          key={d.date}
                          className={`day-chip${d.date === chosen ? ' selected' : ''}`}
                          disabled={!d.available || !canEdit}
                          onClick={() => setDate(d.date === a.date ? null : d.date)}
                        >
                          <strong>{d.label}</strong>
                          <span>
                            {d.occupied}/{d.cupo}
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </>
            )
          )}
        </QueryState>

        {canEdit && a && !a.outOfCoverage && (
          <form
            className="stack"
            onSubmit={(e) => {
              e.preventDefault();
              schedule.mutate(undefined);
            }}
          >
            <Field label="Cuándo prefiere">
              <div className="segmented">
                {TURNOS.map((t) => (
                  <button
                    type="button"
                    key={t.key}
                    className={`segmented__option${turno === t.key ? ' selected' : ''}`}
                    onClick={() => setTurno(t.key)}
                  >
                    {t.name}
                  </button>
                ))}
              </div>
            </Field>
            <div className="form-grid">
              {turno === 'HoraEspecifica' && (
                <Field label="Hora específica">
                  <input type="time" value={specificTime} onChange={(e) => setSpecificTime(e.target.value)} required />
                </Field>
              )}
              <Field label="Prendas aproximadas">
                <input type="number" min={0} max={500} value={garments} onChange={(e) => setGarments(e.target.value)} />
              </Field>
              <Field label="Hora de registro" hint="Vacía = ahora. Cámbiala para una llamada de antes del corte.">
                <input type="datetime-local" value={registeredAt} onChange={(e) => setRegisteredAt(e.target.value)} />
              </Field>
            </div>
            <Field label="Línea de servicio">
              <div className="actions">
                {lineasServicio.data
                  ?.filter((l) => l.active)
                  .map((l) => (
                    <Checkbox
                      key={l.id}
                      checked={lineas.includes(l.id)}
                      onChange={(on) => setLineas(on ? [...lineas, l.id] : lineas.filter((x) => x !== l.id))}
                    >
                      {l.name}
                    </Checkbox>
                  ))}
              </div>
            </Field>
            <Field label="Notas para el chofer">
              <input value={notes} onChange={(e) => setNotes(e.target.value)} maxLength={500} />
            </Field>
            <Checkbox checked={priority} onChange={setPriority}>
              Prioridad
            </Checkbox>
            {notice && <div className="alert info">{notice}</div>}
            {schedule.error && <div className="alert error">{schedule.error.message}</div>}
            <div className="actions end">
              <button type="submit" className="primary" disabled={schedule.isPending || !chosen}>
                {schedule.isPending ? 'Agendando…' : chosen ? `Agendar el ${formatDate(chosen)}` : 'Agendar'}
              </button>
            </div>
          </form>
        )}
      </div>
    </section>
  );
}

// ---- Scheduled pedidos ----

function PedidoList() {
  const isAdmin = useIsAdministrator();
  const pedidos = useQuery({ queryKey: keys.pedidos, queryFn: () => unwrap(api.GET('/pedidos')) });
  const remove = useAction(
    (id: number) => unwrap(api.DELETE('/pedidos/{id}', { params: { path: { id } } })),
    [keys.pedidos, ['agenda']],
    'Pedido eliminado; su lugar quedó libre.',
  );

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Recolecciones agendadas</h2>
          <p className="muted">De hoy en adelante.</p>
        </div>
      </div>
      <QueryState loading={pedidos.isPending} error={pedidos.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Invitado</th>
                <th>Dirección</th>
                <th>Zona</th>
                <th>Cuándo</th>
                <th>Servicio</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pedidos.data?.map((p) => (
                <tr key={p.id}>
                  <td>
                    <strong>{formatDate(p.recoleccionDate)}</strong>
                  </td>
                  <td>
                    {p.invitadoName}
                    <div className="muted">{p.whatsAppPhone}</div>
                  </td>
                  <td>{p.address}</td>
                  <td>
                    {p.zonaCode ?? '—'}
                    {p.rutaCode && <div className="muted">Ruta {p.rutaCode}</div>}
                  </td>
                  <td>
                    {turnoName(p.turno)}
                    {p.specificTime && ` ${p.specificTime.slice(0, 5)}`}
                    {p.priority && (
                      <>
                        {' '}
                        <span className="badge danger">Prioridad</span>
                      </>
                    )}
                  </td>
                  <td>
                    {p.lineasServicio.join(', ') || '—'}
                    {p.garmentsApprox != null && <div className="muted">{p.garmentsApprox} prendas</div>}
                  </td>
                  <td className="end">
                    {isAdmin && (
                      <button
                        type="button"
                        className="small danger"
                        disabled={remove.isPending}
                        onClick={() =>
                          confirm(`¿Eliminar la recolección de ${p.invitadoName} del ${formatDate(p.recoleccionDate)}? Su lugar queda libre.`) &&
                          remove.mutate(p.id)
                        }
                      >
                        Eliminar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {pedidos.data?.length === 0 && <div className="empty">No hay recolecciones agendadas de hoy en adelante.</div>}
        </div>
      </QueryState>
    </section>
  );
}

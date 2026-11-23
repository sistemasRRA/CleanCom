import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { useSession } from '../../session/Session';
import { ActiveBadge, Checkbox, Dialog, Field, PageHeader, QueryState, optional, optionalNumber, useAction, useSave } from '../../components/Common';
import { useConfirm } from '../../components/Toasts';
import { LABEL as TYPE_LABEL, UnidadIcon } from '../../components/UnidadIcon';

type Card = Schemas['UnidadCardDto'];
type Detail = Schemas['UnidadDetailDto'];
type DocumentKind = Schemas['UnidadDocumentKind'];
type EventKind = Schemas['UnidadEventKind'];
type Severity = NonNullable<Schemas['FailureSeverity']>;
type Frequency = Schemas['PaymentFrequency'];
type AlertEvent = Schemas['FleetAlertEvent'];

const listKey = ['unidades'] as const;
const detailKey = (id: number) => ['unidades', id] as const;
const rulesKey = ['avisos-flota'] as const;

const EVENT: Record<EventKind, string> = {
  Servicio: 'Servicio',
  Reparacion: 'Reparación',
  Falla: 'Falla',
  Pago: 'Pago',
  Kilometraje: 'Kilometraje',
};
const DOCUMENT: Record<DocumentKind, string> = {
  Seguro: 'Póliza de seguro',
  TarjetaCirculacion: 'Tarjeta de circulación',
  Verificacion: 'Verificación',
  Comprobante: 'Comprobante',
  Otro: 'Otro',
  Foto: 'Foto de la unidad',
};
const SEVERITY: Record<Severity, string> = { Baja: 'Baja', Media: 'Media', Alta: 'Alta' };
const FREQUENCY: Record<Frequency, string> = {
  Mensual: 'Mensual',
  Bimestral: 'Bimestral',
  Trimestral: 'Trimestral',
  Semestral: 'Semestral',
  Anual: 'Anual',
};
const ALERT_EVENT: Record<AlertEvent, string> = {
  Verificacion: 'Verificación',
  Seguro: 'Seguro',
  PagoRecurrente: 'Pagos recurrentes',
  TarjetaCirculacion: 'Tarjeta de circulación',
  Servicio: 'Servicio',
  Falla: 'Falla reportada',
};
const LEVEL_TONE: Record<Schemas['FleetAlertLevel'], string> = { Vencido: 'danger', Falla: 'danger', PorVencer: 'warning', SinRegistro: '' };

const money = (n?: number | null) => (n == null ? '—' : n.toLocaleString('es-MX', { style: 'currency', currency: 'MXN', maximumFractionDigits: 2 }));
const km = (n?: number | null) => (n == null ? '—' : `${n.toLocaleString('es-MX')} km`);
const day = (iso?: string | null) =>
  iso ? new Date(`${iso.slice(0, 10)}T12:00:00Z`).toLocaleDateString('es-MX', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }) : '—';
const today = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Mexico_City' }).format(new Date());

/**
 * Unidades (docs/pantallas/oficina/unidades.md): one card per vehicle with its notices, service and the year's spending.
 * The notices, the next service and the costs come computed from the API; the screen only shows and sends data.
 */
export function Unidades() {
  const canEdit = useSession().can('unidades', 'edit');
  const [includeInactive, setIncludeInactive] = useState(false);
  const [open, setOpen] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [rules, setRules] = useState(false);
  const list = useQuery({
    queryKey: [...listKey, includeInactive],
    queryFn: () => unwrap(api.GET('/unidades', { params: { query: { includeInactive } } })),
  });

  return (
    <>
      <PageHeader title="Unidades" description="Verificación, seguro, tarjeta de circulación, servicios, fallas y pagos de cada unidad.">
        <Checkbox checked={includeInactive} onChange={setIncludeInactive}>
          Ver dadas de baja
        </Checkbox>
        <button type="button" onClick={() => setRules(true)}>
          Reglas de aviso
        </button>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setCreating(true)}>
            Nueva unidad
          </button>
        )}
      </PageHeader>

      <QueryState loading={list.isLoading} error={list.error}>
        {list.data && (
          <>
            {list.data.withPending > 0 && (
              <div className="alert">
                {list.data.withPending} unidad{list.data.withPending === 1 ? '' : 'es'} con pendientes.
              </div>
            )}
            {list.data.unidades.length === 0 ? (
              <div className="empty">No hay unidades registradas.{canEdit && ' Agrega la primera con "Nueva unidad".'}</div>
            ) : (
              <div className="unidad-cards">
                {list.data.unidades.map((u) => (
                  <UnidadCard key={u.id} unidad={u} onOpen={() => setOpen(u.id)} />
                ))}
              </div>
            )}
          </>
        )}
      </QueryState>

      {open !== null && <UnidadDialog id={open} canEdit={canEdit} onClose={() => setOpen(null)} />}
      {creating && <UnidadForm onClose={() => setCreating(false)} onSaved={(d) => (setCreating(false), setOpen(d.card.id))} />}
      {rules && <RulesDialog canEdit={canEdit} onClose={() => setRules(false)} />}
    </>
  );
}

function UnidadCard({ unidad: u, onOpen }: { unidad: Card; onOpen: () => void }) {
  return (
    <button type="button" className={`unidad-card${u.pending ? ' pending' : ''}${u.active ? '' : ' inactive'}`} onClick={onOpen}>
      <div className="unidad-card__head">
        <UnidadAvatar unidad={u} />
        <div>
          <header>
            <strong className="unidad-plate">{u.plate}</strong>
            {u.state === 'FueraDeServicio' && <span className="badge danger">Fuera de servicio</span>}
            {!u.active && <ActiveBadge active={false} />}
          </header>
          <span className="muted">
            {u.brand} {u.model} {u.year} · {km(u.km)}
            {u.kmUpdatedAt && ` al ${day(u.kmUpdatedAt)}`}
          </span>
        </div>
      </div>
      {u.alerts.length > 0 && (
        <ul className="unidad-alerts">
          {u.alerts.map((a, i) => (
            <li key={i} className={LEVEL_TONE[a.level]}>
              {a.message}
            </li>
          ))}
        </ul>
      )}
      <dl className="unidad-facts">
        <dt>Último servicio</dt>
        <dd>{u.lastServiceOn ? `${day(u.lastServiceOn)} · ${km(u.lastServiceKm)}` : 'Sin registro'}</dd>
        <dt>Próximo servicio</dt>
        <dd>{u.nextServiceOn || u.nextServiceKm ? `${day(u.nextServiceOn)} o ${km(u.nextServiceKm)}` : '—'}</dd>
        <dt>Seguro</dt>
        <dd>{u.insuranceUntil ? `hasta ${day(u.insuranceUntil)}` : '—'}</dd>
        <dt>Tarjeta</dt>
        <dd>{u.circulationCardUntil ? `hasta ${day(u.circulationCardUntil)}` : '—'}</dd>
        <dt>Verificación</dt>
        <dd>{u.verificationExempt ? 'Exenta' : u.lastVerifiedOn ? day(u.lastVerifiedOn) : '—'}</dd>
        <dt>Gasto del año</dt>
        <dd>
          {money(u.spentThisYear)}
          {u.costPerKm != null && ` · ${money(u.costPerKm)}/km`}
        </dd>
      </dl>
    </button>
  );
}

/** The unidad's photo, or the icon of its type while it has none. */
function UnidadAvatar({ unidad: u }: { unidad: Card }) {
  const photo = useQuery({
    queryKey: ['unidades', u.id, 'foto', u.photoId],
    enabled: u.photoId != null,
    staleTime: Infinity,
    queryFn: async () =>
      URL.createObjectURL(
        (await unwrap(
          api.GET('/unidades/{id}/documentos/{documentoId}/archivo', { params: { path: { id: u.id, documentoId: u.photoId! } }, parseAs: 'blob' }),
        )) as Blob,
      ),
  });
  return (
    <span className={`unidad-avatar type-${u.type.toLowerCase()}`} title={TYPE_LABEL[u.type]}>
      {photo.data ? <img src={photo.data} alt={`Foto de ${u.plate}`} /> : <UnidadIcon type={u.type} size={56} />}
    </span>
  );
}

type Action = 'edit' | 'event' | 'document' | 'photo' | { payment: Schemas['RecurringPaymentDto'] | null } | null;

function UnidadDialog({ id, canEdit, onClose }: { id: number; canEdit: boolean; onClose: () => void }) {
  const detail = useQuery({ queryKey: detailKey(id), queryFn: () => unwrap(api.GET('/unidades/{id}', { params: { path: { id } } })) });
  const [action, setAction] = useState<Action>(null);
  const [tab, setTab] = useState<'bitacora' | 'documentos' | 'pagos'>('bitacora');
  const resolve = useAction(
    (eventoId: number) => unwrap(api.POST('/unidades/{id}/eventos/{eventoId}/resolver', { params: { path: { id, eventoId } } })),
    [listKey, detailKey(id)],
    'Falla marcada como resuelta.',
  );
  const ask = useConfirm();
  const removeDocument = useAction(
    (documentoId: number) => unwrap(api.DELETE('/unidades/{id}/documentos/{documentoId}', { params: { path: { id, documentoId } } })),
    [listKey, detailKey(id)],
    'Documento dado de baja.',
  );
  const pay = useAction(
    (p: Schemas['RecurringPaymentDto']) =>
      unwrap(
        api.POST('/unidades/{id}/eventos', {
          params: { path: { id } },
          body: { kind: 'Pago', date: today(), km: null, amount: null, description: p.concept, severity: null, recurringPaymentId: p.id },
        }),
      ),
    [listKey, detailKey(id)],
    'Pago registrado; la siguiente fecha se movió.',
  );
  const d = detail.data;

  return (
    <>
    <Dialog title={d ? `Unidad ${d.card.plate}` : 'Unidad'} onClose={onClose} onSubmit={onClose} submitText="Cerrar">
      <QueryState loading={detail.isPending} error={detail.error}>
        {d && (
          <div className="stack unidad-detail">
            <div className="unidad-detail__top">
              <UnidadCard unidad={d.card} onOpen={() => undefined} />
              {canEdit && d.card.active && (
                <div className="actions">
                  <button type="button" onClick={() => setAction('edit')}>
                    Editar datos
                  </button>
                  <button type="button" onClick={() => setAction('event')}>
                    Registrar evento
                  </button>
                  <button type="button" onClick={() => setAction('document')}>
                    Agregar documento
                  </button>
                  <button type="button" onClick={() => setAction('photo')}>
                    {d.card.photoId ? 'Cambiar foto' : 'Agregar foto'}
                  </button>
                  <button type="button" onClick={() => setAction({ payment: null })}>
                    Nuevo pago recurrente
                  </button>
                </div>
              )}
            </div>
            {d.notes && <p className="muted">{d.notes}</p>}

            <div className="tabs" role="tablist">
              {(['bitacora', 'documentos', 'pagos'] as const).map((t) => (
                <button key={t} type="button" role="tab" aria-selected={tab === t} className={tab === t ? 'selected' : ''} onClick={() => setTab(t)}>
                  {t === 'bitacora' ? `Bitácora (${d.events.length})` : t === 'documentos' ? `Documentos (${d.documents.length})` : `Pagos recurrentes (${d.payments.length})`}
                </button>
              ))}
            </div>

            {tab === 'bitacora' &&
              (d.events.length === 0 ? (
                <div className="empty">Sin registros en la bitácora.</div>
              ) : (
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Fecha</th>
                        <th>Tipo</th>
                        <th>Descripción</th>
                        <th>Km</th>
                        <th>Monto</th>
                        <th />
                      </tr>
                    </thead>
                    <tbody>
                      {d.events.map((e) => (
                        <tr key={e.id}>
                          <td>{day(e.date)}</td>
                          <td>
                            {EVENT[e.kind]}
                            {e.severity && <span className="badge danger"> {SEVERITY[e.severity]}</span>}
                          </td>
                          <td>{e.description}</td>
                          <td>{e.km != null ? km(e.km) : ''}</td>
                          <td>{e.amount != null ? money(e.amount) : ''}</td>
                          <td>
                            {e.kind === 'Falla' &&
                              (e.resolvedOn ? (
                                <span className="muted">Resuelta {day(e.resolvedOn)}</span>
                              ) : canEdit ? (
                                <button type="button" className="small" disabled={resolve.isPending} onClick={() => resolve.mutate(e.id)}>
                                  Marcar resuelta
                                </button>
                              ) : (
                                <span className="badge danger">Abierta</span>
                              ))}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ))}

            {tab === 'documentos' &&
              (d.documents.length === 0 ? (
                <div className="empty">Sin documentos.</div>
              ) : (
                <ul className="plain-list unidad-docs">
                  {d.documents.map((doc) => (
                    <li key={doc.id}>
                      <span>
                        <strong>{DOCUMENT[doc.kind]}</strong>
                        {doc.kind === 'Verificacion' ? ` · hecha el ${day(doc.validFrom)}` : doc.validUntil ? ` · vigente hasta ${day(doc.validUntil)}` : ''}
                        {doc.description && ` · ${doc.description}`}
                      </span>
                      <span className="actions tight">
                        {doc.hasFile && <DocumentLink unidadId={id} documentoId={doc.id} />}
                        {canEdit && (
                          <button type="button" className="small" onClick={async () => (await ask('¿Dar de baja este documento?', { confirmText: 'Dar de baja', danger: true })) && removeDocument.mutate(doc.id)}>
                            Quitar
                          </button>
                        )}
                      </span>
                    </li>
                  ))}
                </ul>
              ))}

            {tab === 'pagos' &&
              (d.payments.length === 0 ? (
                <div className="empty">Sin pagos recurrentes (tenencia, crédito, póliza en pagos).</div>
              ) : (
                <ul className="plain-list unidad-docs">
                  {d.payments.map((p) => (
                    <li key={p.id}>
                      <span>
                        <strong>{p.concept}</strong> · {money(p.amount)} · {FREQUENCY[p.frequency].toLowerCase()} · próximo {day(p.nextDate)}
                      </span>
                      {canEdit && (
                        <span className="actions tight">
                          <button type="button" className="small" disabled={pay.isPending} onClick={async () => (await ask(`¿Registrar hoy el pago de ${p.concept} por ${money(p.amount)}?`, { confirmText: 'Registrar pago' })) && pay.mutate(p)}>
                            Registrar pago
                          </button>
                          <button type="button" className="small" onClick={() => setAction({ payment: p })}>
                            Editar
                          </button>
                        </span>
                      )}
                    </li>
                  ))}
                </ul>
              ))}
          </div>
        )}
      </QueryState>
    </Dialog>

    {/* Outside the detail: a form inside another form would submit (close) it too. */}
    {d && action === 'edit' && <UnidadForm detail={d} onClose={() => setAction(null)} onSaved={() => setAction(null)} />}
    {d && action === 'event' && <EventForm detail={d} onClose={() => setAction(null)} />}
    {d && action === 'document' && <DocumentForm detail={d} onClose={() => setAction(null)} />}
    {d && action === 'photo' && <DocumentForm detail={d} photo onClose={() => setAction(null)} />}
    {d && action && typeof action === 'object' && <PaymentForm unidadId={id} payment={action.payment} onClose={() => setAction(null)} />}
    </>
  );
}

/** Opens a document with the session (the file is behind the API's permissions). */
function DocumentLink({ unidadId, documentoId }: { unidadId: number; documentoId: number }) {
  const [busy, setBusy] = useState(false);
  async function open() {
    setBusy(true);
    try {
      const blob = (await unwrap(
        api.GET('/unidades/{id}/documentos/{documentoId}/archivo', { params: { path: { id: unidadId, documentoId } }, parseAs: 'blob' }),
      )) as Blob;
      window.open(URL.createObjectURL(blob), '_blank', 'noopener');
    } finally {
      setBusy(false);
    }
  }
  return (
    <button type="button" className="small" disabled={busy} onClick={() => void open()}>
      {busy ? 'Abriendo…' : 'Ver archivo'}
    </button>
  );
}

function UnidadForm({ detail, onClose, onSaved }: { detail?: Detail; onClose: () => void; onSaved: (d: Detail) => void }) {
  const c = detail?.card;
  const [f, setF] = useState({
    plate: c?.plate ?? '',
    brand: c?.brand ?? '',
    model: c?.model ?? '',
    year: String(c?.year ?? new Date().getFullYear()),
    km: String(c?.km ?? 0),
    garmentCapacity: detail?.garmentCapacity != null ? String(detail.garmentCapacity) : '',
    serviceEveryKm: detail?.serviceEveryKm != null ? String(detail.serviceEveryKm) : '',
    serviceEveryMonths: detail?.serviceEveryMonths != null ? String(detail.serviceEveryMonths) : '',
    outOfService: c?.state === 'FueraDeServicio',
    verificationExempt: c?.verificationExempt ?? false,
    type: c?.type ?? ('Otro' as Schemas['UnidadType']),
    notes: detail?.notes ?? '',
    active: c?.active ?? true,
  });
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const body: Schemas['SaveUnidadRequest'] = {
    plate: f.plate,
    brand: f.brand,
    model: f.model,
    year: Number(f.year),
    km: Number(f.km),
    garmentCapacity: optionalNumber(f.garmentCapacity),
    serviceEveryKm: optionalNumber(f.serviceEveryKm),
    serviceEveryMonths: optionalNumber(f.serviceEveryMonths),
    state: f.outOfService ? 'FueraDeServicio' : 'Activa',
    notes: optional(f.notes),
    active: f.active,
    verificationExempt: f.verificationExempt,
    type: f.type,
  };
  const save = useSave(
    () =>
      c
        ? unwrap(api.PUT('/unidades/{id}', { params: { path: { id: c.id } }, body }))
        : unwrap(api.POST('/unidades', { body })),
    { refetch: [listKey], message: c ? 'Unidad guardada.' : 'Unidad agregada.', onDone: (d) => onSaved(d as Detail) },
  );

  return (
    <Dialog title={c ? `Editar ${c.plate}` : 'Nueva unidad'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Placa" hint="Se guarda sin espacios ni guiones.">
          <input value={f.plate} onChange={(e) => set({ plate: e.target.value })} required maxLength={15} />
        </Field>
        <Field label="Tipo">
          <select value={f.type} onChange={(e) => set({ type: e.target.value as Schemas['UnidadType'] })}>
            {(Object.keys(TYPE_LABEL) as Schemas['UnidadType'][]).map((t) => (
              <option key={t} value={t}>
                {TYPE_LABEL[t]}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Marca">
          <input value={f.brand} onChange={(e) => set({ brand: e.target.value })} required maxLength={60} />
        </Field>
        <Field label="Modelo">
          <input value={f.model} onChange={(e) => set({ model: e.target.value })} required maxLength={60} />
        </Field>
        <Field label="Año">
          <input type="number" value={f.year} onChange={(e) => set({ year: e.target.value })} required min={1980} max={2100} />
        </Field>
        <Field label="Kilometraje">
          <input type="number" value={f.km} onChange={(e) => set({ km: e.target.value })} required min={0} />
        </Field>
        <Field label="Capacidad de prendas" hint="Vacío: la de Operación.">
          <input type="number" value={f.garmentCapacity} onChange={(e) => set({ garmentCapacity: e.target.value })} min={1} />
        </Field>
        <Field label="Servicio cada (km)" hint="Vacío: el de Operación.">
          <input type="number" value={f.serviceEveryKm} onChange={(e) => set({ serviceEveryKm: e.target.value })} min={1} />
        </Field>
        <Field label="Servicio cada (meses)" hint="Vacío: el de Operación.">
          <input type="number" value={f.serviceEveryMonths} onChange={(e) => set({ serviceEveryMonths: e.target.value })} min={1} max={60} />
        </Field>
        <Field label="Notas" wide>
          <textarea value={f.notes} onChange={(e) => set({ notes: e.target.value })} maxLength={1000} rows={2} />
        </Field>
        <Checkbox checked={f.outOfService} onChange={(v) => set({ outOfService: v })} wide>
          Fuera de servicio
        </Checkbox>
        <Checkbox checked={f.verificationExempt} onChange={(v) => set({ verificationExempt: v })} wide>
          Exenta de verificación (por ejemplo, las trimotos)
        </Checkbox>
        {c && (
          <Checkbox checked={!f.active} onChange={(v) => set({ active: !v })} wide>
            Dar de baja (deja de aparecer; no se borra)
          </Checkbox>
        )}
      </div>
    </Dialog>
  );
}

function EventForm({ detail, onClose }: { detail: Detail; onClose: () => void }) {
  const id = detail.card.id;
  const [f, setF] = useState({ kind: 'Servicio' as EventKind, date: today(), km: '', amount: '', description: '', severity: 'Media' as Severity });
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const save = useSave(
    () =>
      unwrap(
        api.POST('/unidades/{id}/eventos', {
          params: { path: { id } },
          body: {
            kind: f.kind,
            date: f.date,
            km: optionalNumber(f.km),
            amount: optionalNumber(f.amount),
            description: optional(f.description),
            severity: f.kind === 'Falla' ? f.severity : null,
            recurringPaymentId: null,
          },
        }),
      ),
    { refetch: [listKey, detailKey(id)], message: 'Registrado en la bitácora.', onDone: onClose },
  );
  return (
    <Dialog title={`Registrar en ${detail.card.plate}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Tipo">
          <select value={f.kind} onChange={(e) => set({ kind: e.target.value as EventKind })}>
            {(Object.keys(EVENT) as EventKind[]).map((k) => (
              <option key={k} value={k}>
                {EVENT[k]}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Fecha">
          <input type="date" value={f.date} max={today()} onChange={(e) => set({ date: e.target.value })} required />
        </Field>
        <Field label="Kilometraje" hint={f.kind === 'Kilometraje' ? 'Obligatorio.' : `Actual: ${km(detail.card.km)}.`}>
          <input type="number" value={f.km} min={0} onChange={(e) => set({ km: e.target.value })} required={f.kind === 'Kilometraje'} />
        </Field>
        {f.kind !== 'Kilometraje' && (
          <Field label="Monto">
            <input type="number" step="0.01" min={0} value={f.amount} onChange={(e) => set({ amount: e.target.value })} />
          </Field>
        )}
        {f.kind === 'Falla' && (
          <Field label="Gravedad">
            <select value={f.severity} onChange={(e) => set({ severity: e.target.value as Severity })}>
              {(Object.keys(SEVERITY) as Severity[]).map((s) => (
                <option key={s} value={s}>
                  {SEVERITY[s]}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Descripción" wide>
          <textarea value={f.description} onChange={(e) => set({ description: e.target.value })} maxLength={1000} rows={2} required={f.kind === 'Falla' || f.kind === 'Reparacion'} />
        </Field>
      </div>
    </Dialog>
  );
}

/** A document, or with `photo` only the unidad's picture (an image, required). */
function DocumentForm({ detail, photo = false, onClose }: { detail: Detail; photo?: boolean; onClose: () => void }) {
  const id = detail.card.id;
  const [f, setF] = useState({ kind: (photo ? 'Foto' : 'Seguro') as DocumentKind, validFrom: '', validUntil: '', description: '', eventId: '' });
  const [file, setFile] = useState<File | null>(null);
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const save = useSave(
    () =>
      unwrap(
        api.POST('/unidades/{id}/documentos', {
          params: { path: { id } },
          // Multipart: the generated type does not describe the file, so the form data is built here.
          body: {} as never,
          bodySerializer: () => {
            const data = new FormData();
            data.append('Kind', f.kind);
            if (f.validFrom) data.append('ValidFrom', f.validFrom);
            if (f.validUntil) data.append('ValidUntil', f.validUntil);
            if (f.description.trim()) data.append('Description', f.description.trim());
            if (f.eventId) data.append('EventId', f.eventId);
            if (file) data.append('File', file);
            return data;
          },
        }),
      ),
    { refetch: [listKey, detailKey(id)], message: photo ? 'Foto guardada.' : 'Documento agregado.', onDone: onClose },
  );
  const needsUntil = f.kind === 'Seguro' || f.kind === 'TarjetaCirculacion';
  if (photo) {
    return (
      <Dialog title={`Foto de ${detail.card.plate}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
        <Field label="Foto" hint="JPG, PNG o WebP, hasta 15 MB. Se ve en la tarjeta de la unidad.">
          <input type="file" accept="image/jpeg,image/png,image/webp" required onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </Field>
      </Dialog>
    );
  }
  return (
    <Dialog title={`Documento de ${detail.card.plate}`} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Tipo">
          <select value={f.kind} onChange={(e) => set({ kind: e.target.value as DocumentKind })}>
            {(Object.keys(DOCUMENT) as DocumentKind[]).filter((k) => k !== 'Foto').map((k) => (
              <option key={k} value={k}>
                {DOCUMENT[k]}
              </option>
            ))}
          </select>
        </Field>
        <Field label={f.kind === 'Verificacion' ? 'Fecha de la verificación' : 'Vigente desde'}>
          <input type="date" value={f.validFrom} onChange={(e) => set({ validFrom: e.target.value })} required={f.kind === 'Verificacion'} />
        </Field>
        {f.kind !== 'Verificacion' && (
          <Field label="Vigente hasta">
            <input type="date" value={f.validUntil} onChange={(e) => set({ validUntil: e.target.value })} required={needsUntil} />
          </Field>
        )}
        {f.kind === 'Comprobante' && detail.events.length > 0 && (
          <Field label="Del registro">
            <select value={f.eventId} onChange={(e) => set({ eventId: e.target.value })}>
              <option value="">Ninguno</option>
              {detail.events.map((e) => (
                <option key={e.id} value={e.id}>
                  {day(e.date)} · {EVENT[e.kind]} · {e.description}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Descripción" wide>
          <input value={f.description} onChange={(e) => set({ description: e.target.value })} maxLength={300} placeholder="Aseguradora, número de póliza…" />
        </Field>
        <Field label="Archivo" hint="PDF, JPG, PNG o WebP, hasta 15 MB. Opcional." wide>
          <input type="file" accept="application/pdf,image/jpeg,image/png,image/webp" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </Field>
      </div>
    </Dialog>
  );
}

function PaymentForm({ unidadId, payment, onClose }: { unidadId: number; payment: Schemas['RecurringPaymentDto'] | null; onClose: () => void }) {
  const [f, setF] = useState({
    concept: payment?.concept ?? '',
    amount: payment ? String(payment.amount) : '',
    frequency: payment?.frequency ?? ('Anual' as Frequency),
    nextDate: payment?.nextDate ?? today(),
    active: payment?.active ?? true,
  });
  const set = (patch: Partial<typeof f>) => setF((x) => ({ ...x, ...patch }));
  const body: Schemas['SaveRecurringPaymentRequest'] = { concept: f.concept, amount: Number(f.amount), frequency: f.frequency, nextDate: f.nextDate, active: f.active };
  const save = useSave(
    () =>
      payment
        ? unwrap(api.PUT('/unidades/{id}/pagos/{pagoId}', { params: { path: { id: unidadId, pagoId: payment.id } }, body }))
        : unwrap(api.POST('/unidades/{id}/pagos', { params: { path: { id: unidadId } }, body })),
    { refetch: [listKey, detailKey(unidadId)], message: 'Pago recurrente guardado.', onDone: onClose },
  );
  return (
    <Dialog title={payment ? 'Editar pago recurrente' : 'Nuevo pago recurrente'} onClose={onClose} onSubmit={() => save.mutate(undefined)} busy={save.isPending} error={save.error}>
      <div className="form-grid">
        <Field label="Concepto" wide>
          <input value={f.concept} onChange={(e) => set({ concept: e.target.value })} required maxLength={150} placeholder="Tenencia, crédito, póliza en pagos…" />
        </Field>
        <Field label="Monto">
          <input type="number" step="0.01" min={0.01} value={f.amount} onChange={(e) => set({ amount: e.target.value })} required />
        </Field>
        <Field label="Periodicidad">
          <select value={f.frequency} onChange={(e) => set({ frequency: e.target.value as Frequency })}>
            {(Object.keys(FREQUENCY) as Frequency[]).map((k) => (
              <option key={k} value={k}>
                {FREQUENCY[k]}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Próxima fecha">
          <input type="date" value={f.nextDate} onChange={(e) => set({ nextDate: e.target.value })} required />
        </Field>
        {payment && (
          <Checkbox checked={!f.active} onChange={(v) => set({ active: !v })} wide>
            Ya no se paga (dar de baja)
          </Checkbox>
        )}
      </div>
    </Dialog>
  );
}

/** Days, recipients and channels of each fleet notice (GET|PUT /avisos-flota/reglas). */
function RulesDialog({ canEdit, onClose }: { canEdit: boolean; onClose: () => void }) {
  const rules = useQuery({ queryKey: rulesKey, queryFn: () => unwrap(api.GET('/avisos-flota/reglas')) });
  const [edited, setEdited] = useState<Schemas['FleetAlertRuleDto'][] | null>(null);
  const rows = edited ?? rules.data ?? [];
  const set = (i: number, patch: Partial<Schemas['FleetAlertRuleDto']>) => setEdited(rows.map((r, j) => (j === i ? { ...r, ...patch } : r)));
  const save = useSave(() => unwrap(api.PUT('/avisos-flota/reglas', { body: rows })), { refetch: [rulesKey, listKey], message: 'Reglas de aviso guardadas.', onDone: onClose });
  return (
    <Dialog
      title="Reglas de aviso de flota"
      onClose={onClose}
      onSubmit={() => (canEdit ? save.mutate(undefined) : onClose())}
      busy={save.isPending}
      error={save.error}
      submitText={canEdit ? 'Guardar' : 'Cerrar'}
    >
      <QueryState loading={rules.isLoading} error={rules.error} inline>
        <p className="muted">
          Días antes de la fecha en que sale el aviso, separados por coma (0 es el mismo día). Después de la fecha se repite cada 7 días hasta que se
          registre. Los avisos por correo y en la web llegan con las notificaciones (semana 6).
        </p>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Evento</th>
                <th>Días antes</th>
                <th>Destinatarios</th>
                <th>Canales</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={r.event}>
                  <td>{ALERT_EVENT[r.event]}</td>
                  <td>
                    <input value={r.daysBefore} disabled={!canEdit} onChange={(e) => set(i, { daysBefore: e.target.value })} aria-label={`Días antes · ${ALERT_EVENT[r.event]}`} size={8} />
                  </td>
                  <td>
                    <input value={r.recipients} disabled={!canEdit} onChange={(e) => set(i, { recipients: e.target.value })} aria-label={`Destinatarios · ${ALERT_EVENT[r.event]}`} placeholder="Correos o roles" />
                  </td>
                  <td>
                    <input value={r.channels} disabled={!canEdit} onChange={(e) => set(i, { channels: e.target.value })} aria-label={`Canales · ${ALERT_EVENT[r.event]}`} placeholder="Correo,Web,Push" size={14} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
    </Dialog>
  );
}

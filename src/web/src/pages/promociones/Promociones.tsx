import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, unwrap, type Schemas } from '../../api/client';
import { keys } from '../../api/queries';
import { useSession } from '../../session/Session';
import { ActiveBadge, Checkbox, Dialog, Field, PageHeader, QueryState, optional, optionalNumber, useSave } from '../../components/Common';
import { useNotify } from '../../components/Toasts';

type Promocion = Schemas['PromocionDto'];
type Programa = Schemas['PromocionPrograma'];
type DiscountType = Schemas['DiscountType'];
type PaymentMethod = NonNullable<Schemas['PaymentMethod']>;

const boardKey = ['promociones'] as const;

const PROGRAMA: Record<Programa, string> = {
  Sellos: 'Sellos',
  Cuponera: 'Cuponera de bienvenida',
  Campana: 'Campañas',
};
const DISCOUNT: Record<DiscountType, string> = {
  Porcentaje: 'Porcentaje',
  Monto: 'Monto en pesos',
  ArticuloGratis: 'Artículo gratis',
  PrecioEspecial: 'Precio especial',
};
const PAYMENT: Record<PaymentMethod, string> = {
  Efectivo: 'Efectivo',
  Tarjeta: 'Tarjeta',
  Transferencia: 'Transferencia',
};
const validityText = (days?: number | null) => (days == null ? 'Hasta la entrega del pedido' : `${days} días`);
const percent = (n?: number | null) => (n == null ? '—' : `${n.toLocaleString('es-MX', { maximumFractionDigits: 1 })}%`);

/**
 * Promociones (docs/pantallas/oficina/promociones.md): redemption against its goal, sellos and cuponera with their
 * numbers, and marking a beneficio as used by its code. Granting, reserving and the 30% cap are decided by the API.
 */
export function Promociones() {
  const canEdit = useSession().can('promociones', 'edit');
  const [tab, setTab] = useState<Programa>('Sellos');
  const [editing, setEditing] = useState<Promocion | 'new' | null>(null);
  const board = useQuery({
    queryKey: boardKey,
    queryFn: () => unwrap(api.GET('/promociones')),
  });
  const rows = board.data?.promociones.filter((p) => p.programa === tab) ?? [];
  const sellos = tab === 'Sellos';
  const inactiveCupones = tab === 'Cuponera' && rows.some((p) => !p.active);

  return (
    <>
      <PageHeader
        title="Promociones"
        description="Sellos para invitados recurrentes y cuponera de bienvenida. Se entregan solos al cerrar cada recolección; el descuento se aplica en el POS."
      >
        {canEdit && tab !== 'Campana' && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva promoción
          </button>
        )}
      </PageHeader>

      <QueryState loading={board.isPending} error={board.error}>
        {board.data && (
          <>
            <div className="ficha__stats">
              {board.data.indicators.map((i) => (
                <div key={i.programa}>
                  <span className="muted">{i.programa === 'Sellos' ? 'Sellos canjeados' : 'Cuponera canjeada'}</span>
                  <strong>
                    {percent(i.redemptionRate)}{' '}
                    {i.redemptionRate != null && (
                      <span className={`badge ${i.meetsGoal ? 'success' : 'warning'}`}>{i.meetsGoal ? 'en meta' : 'bajo la meta'}</span>
                    )}
                  </strong>
                  <span className="muted">
                    {i.used} de {i.granted} · meta {i.goal}% o más
                  </span>
                </div>
              ))}
              <div>
                <span className="muted">Campañas</span>
                <strong>—</strong>
                <span className="muted">Llegan con las notificaciones</span>
              </div>
            </div>

            <UseByCode />

            <nav className="tabs">
              {(['Sellos', 'Cuponera', 'Campana'] as Programa[]).map((p) => (
                <button key={p} type="button" className={tab === p ? 'selected' : ''} onClick={() => setTab(p)}>
                  {PROGRAMA[p]}
                </button>
              ))}
            </nav>

            {tab === 'Campana' ? (
              <div className="alert">
                Las campañas (segmento del CRM por ruta y estatus, canal push o WhatsApp de marketing y envío programado) llegan con las
                notificaciones.
              </div>
            ) : (
              <section className="card">
                <div className="card__header">
                  <p className="muted">
                    {sellos
                      ? `Se da al cerrar la recolección de un servicio con la línea indicada, a partir del segundo servicio, y no se repite mientras el invitado tenga uno sin usar. Ningún sello debe pasar del ${board.data.selloCap}% de los otorgados.`
                      : 'Al cerrar la recolección de su primer servicio, el invitado recibe un cupón de cada promoción activa. Un cupón inactivo no se entrega.'}
                  </p>
                  {inactiveCupones && (
                    <div className="alert">
                      Captura el beneficio impreso de cada cupón y actívalo: mientras estén inactivos, los invitados nuevos no reciben la cuponera
                      digital.
                    </div>
                  )}
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Promoción</th>
                        <th>Mecánica</th>
                        {sellos && <th>Se da al cerrar</th>}
                        <th>Vigencia</th>
                        <th className="num">Otorgados</th>
                        <th className="num">Usados</th>
                        <th className="num">Apartados</th>
                        <th className="num">Canje</th>
                        {sellos && <th className="num">De los sellos</th>}
                        <th>Estado</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.length === 0 && (
                        <tr>
                          <td colSpan={sellos ? 11 : 9} className="muted">
                            Sin promociones todavía.
                          </td>
                        </tr>
                      )}
                      {rows.map((p) => (
                        <tr key={p.id} className={p.active ? '' : 'inactive'}>
                          <td>
                            <strong>{p.name}</strong>
                            {p.condition && <div className="muted">{p.condition}</div>}
                          </td>
                          <td>{p.mechanic}</td>
                          {sellos && <td>{p.triggerLineaName ? `Servicio de ${p.triggerLineaName}` : 'Cualquier servicio'}</td>}
                          <td>{validityText(p.validityDays)}</td>
                          <td className="num">{p.granted}</td>
                          <td className="num">{p.used}</td>
                          <td className="num">{p.reserved}</td>
                          <td className="num">{percent(p.redemptionRate)}</td>
                          {sellos && (
                            <td className="num">
                              {percent(p.sharePercent)}
                              {p.overCap && (
                                <span className="badge warning" title={`Pasa del ${board.data.selloCap}% de los sellos otorgados`}>
                                  tope
                                </span>
                              )}
                            </td>
                          )}
                          <td>
                            <ActiveBadge active={p.active} />
                          </td>
                          <td className="end">
                            {canEdit && (
                              <button type="button" className="small" onClick={() => setEditing(p)}>
                                Editar
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}
          </>
        )}
      </QueryState>

      {editing && <PromocionForm promocion={editing === 'new' ? null : editing} programa={tab} onClose={() => setEditing(null)} />}
    </>
  );
}

/** "Marcar como usado" by the beneficio's code (the invitado shows it in the app). */
function UseByCode() {
  const canUse = useSession().can('promociones', 'edit');
  const notify = useNotify();
  const [code, setCode] = useState('');
  const use = useSave(
    () =>
      unwrap(
        api.POST('/beneficios/{codigo}/usar', {
          params: { path: { codigo: code.trim() } },
        }),
      ),
    {
      refetch: [boardKey, keys.crm],
      onDone: (b) => {
        notify(`${b.name} (${b.code}) quedó usado.`);
        setCode('');
      },
    },
  );
  if (!canUse) return null;
  return (
    <form
      className="actions"
      style={{ margin: '16px 0' }}
      onSubmit={(e) => {
        e.preventDefault();
        if (code.trim()) use.mutate(undefined);
      }}
    >
      <input
        value={code}
        onChange={(e) => setCode(e.target.value)}
        placeholder="Código del beneficio, p. ej. CC-7KQ4-M2XP"
        aria-label="Código del beneficio"
        style={{ minWidth: 320 }}
      />
      <button type="submit" disabled={use.isPending || !code.trim()}>
        Marcar como usado
      </button>
      {use.error && <span className="alert error">{use.error.message}</span>}
    </form>
  );
}

function PromocionForm({ promocion: p, programa, onClose }: { promocion: Promocion | null; programa: Programa; onClose: () => void }) {
  const lineas = useQuery({
    queryKey: keys.lineasServicio,
    queryFn: () => unwrap(api.GET('/lineas-servicio')),
  });
  const prog = p?.programa ?? programa;
  const [name, setName] = useState(p?.name ?? '');
  const [lineaId, setLineaId] = useState(p?.lineaServicioId?.toString() ?? '');
  const [discountType, setDiscountType] = useState<DiscountType>(p?.discountType ?? 'Porcentaje');
  const [value, setValue] = useState(p ? String(p.value) : '');
  const [minimum, setMinimum] = useState(p?.minimumAmount?.toString() ?? '');
  const [payment, setPayment] = useState<PaymentMethod | ''>(p?.paymentMethod ?? '');
  const [stackable, setStackable] = useState(p?.stackable ?? false);
  const [triggerId, setTriggerId] = useState(p?.triggerLineaServicioId?.toString() ?? '');
  const [description, setDescription] = useState(p?.description ?? '');
  const [condition, setCondition] = useState(p?.condition ?? '');
  // Sellos can last "hasta la entrega del pedido" that gave them; the rest last some days.
  const [untilEntrega, setUntilEntrega] = useState(prog === 'Sellos' && (p ? p.validityDays == null : true));
  const [validity, setValidity] = useState(String(p?.validityDays ?? (prog === 'Cuponera' ? 60 : 30)));
  const [active, setActive] = useState(p?.active ?? true);

  const save = useSave(
    () => {
      const body: Schemas['SavePromocionRequest'] = {
        programa: prog,
        name: name.trim(),
        lineaServicioId: lineaId ? Number(lineaId) : null,
        discountType,
        value: discountType === 'ArticuloGratis' ? 0 : Number(value || 0),
        minimumAmount: optionalNumber(minimum),
        paymentMethod: payment || null,
        stackable,
        triggerLineaServicioId: prog === 'Sellos' && triggerId ? Number(triggerId) : null,
        description: optional(description),
        condition: optional(condition),
        validityDays: prog === 'Sellos' && untilEntrega ? null : Number(validity || 0),
        active,
      };
      return p
        ? unwrap(
            api.PUT('/promociones/{id}', {
              params: { path: { id: p.id } },
              body,
            }),
          )
        : unwrap(api.POST('/promociones', { body }));
    },
    { refetch: [boardKey], message: 'Promoción guardada.', onDone: onClose },
  );
  const lineaOptions = (lineas.data ?? []).filter((l) => l.active || String(l.id) === lineaId || String(l.id) === triggerId);

  return (
    <Dialog
      title={p ? `Editar ${p.name}` : `Nueva promoción · ${PROGRAMA[prog]}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <div className="form-grid">
        <Field label="Nombre" wide>
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} placeholder="Edredón o cobija al 20%" />
        </Field>
        <Field label="Línea del descuento" hint={prog === 'Cuponera' ? 'Cada cupón es de una línea.' : 'Vacío = cualquier línea.'}>
          <select value={lineaId} onChange={(e) => setLineaId(e.target.value)} required={prog === 'Cuponera'}>
            <option value="">{prog === 'Cuponera' ? 'Elige la línea…' : 'Cualquier línea'}</option>
            {lineaOptions.map((l) => (
              <option key={l.id} value={l.id}>
                {l.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Tipo de descuento">
          <select value={discountType} onChange={(e) => setDiscountType(e.target.value as DiscountType)}>
            {(Object.keys(DISCOUNT) as DiscountType[]).map((d) => (
              <option key={d} value={d}>
                {DISCOUNT[d]}
              </option>
            ))}
          </select>
        </Field>
        {discountType !== 'ArticuloGratis' && (
          <Field label={discountType === 'Porcentaje' ? 'Porcentaje' : discountType === 'PrecioEspecial' ? 'Precio ($)' : 'Monto ($)'}>
            <input
              type="number"
              min={1}
              max={discountType === 'Porcentaje' ? 100 : undefined}
              step="0.01"
              value={value}
              onChange={(e) => setValue(e.target.value)}
              required
            />
          </Field>
        )}
        <Field label="Mínimo del ticket ($)" hint="Vacío = sin mínimo.">
          <input type="number" min={1} step="0.01" value={minimum} onChange={(e) => setMinimum(e.target.value)} />
        </Field>
        <Field label="Forma de pago">
          <select value={payment} onChange={(e) => setPayment(e.target.value as PaymentMethod | '')}>
            <option value="">Cualquiera</option>
            {(Object.keys(PAYMENT) as PaymentMethod[]).map((m) => (
              <option key={m} value={m}>
                {PAYMENT[m]}
              </option>
            ))}
          </select>
        </Field>
        {prog === 'Sellos' && (
          <Field label="Vigencia">
            <select value={untilEntrega ? 'entrega' : 'dias'} onChange={(e) => setUntilEntrega(e.target.value === 'entrega')}>
              <option value="entrega">Hasta la entrega del pedido</option>
              <option value="dias">Días desde que se da</option>
            </select>
          </Field>
        )}
        {!(prog === 'Sellos' && untilEntrega) && (
          <Field label="Vigencia (días)" hint="Desde que se entrega.">
            <input type="number" min={1} max={365} value={validity} onChange={(e) => setValidity(e.target.value)} required />
          </Field>
        )}
        {prog === 'Sellos' && (
          <Field label="Se da al cerrar un servicio de" hint="A partir del segundo servicio del invitado.">
            <select value={triggerId} onChange={(e) => setTriggerId(e.target.value)}>
              <option value="">Cualquier servicio</option>
              {lineaOptions.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Mecánica" hint="Como la lee el invitado. Vacío = se arma con las reglas de arriba." wide>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            maxLength={300}
            rows={2}
            placeholder="20% de descuento en el lavado de un edredón, cobija o prenda voluminosa. Pago de contado."
          />
        </Field>
        <Field label="Condición" hint="La ven el invitado y el chofer." wide>
          <input value={condition} onChange={(e) => setCondition(e.target.value)} maxLength={500} placeholder="Un edredón o cobija por pedido." />
        </Field>
        <Checkbox checked={stackable} onChange={setStackable}>
          Acumulable con otros beneficios
        </Checkbox>
        <Checkbox checked={active} onChange={setActive}>
          Activa (se sigue entregando)
        </Checkbox>
      </div>
    </Dialog>
  );
}

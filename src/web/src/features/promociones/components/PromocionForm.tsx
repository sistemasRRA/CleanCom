import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { DISCOUNT, type DiscountType, PAYMENT, type PaymentMethod, type Programa, PROGRAMA, type Promocion } from '../types/promociones';
import { keys } from '../../../shared/api/queryKeys';
import { type Schemas } from '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { optional, optionalNumber } from '../../../shared/utils/format';
import { boardKey } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { promocionesApi } from '../services/promocionesApi';
import { operacionApi } from '../../operacion';

export function PromocionForm({ promocion: p, programa, onClose }: { promocion: Promocion | null; programa: Programa; onClose: () => void }) {
  const lineas = useQuery({
    queryKey: keys.lineasServicio,
    queryFn: () => operacionApi.listLineasServicio(),
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
      return p ? promocionesApi.update(p.id, body) : promocionesApi.create(body);
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

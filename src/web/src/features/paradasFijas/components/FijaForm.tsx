import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import {
  ACTIONS,
  CATEGORIES,
  type Category,
  categoryOf,
  emptyPaso,
  type Fija,
  type Kind,
  type PasoForm,
  type Placement,
  PLACEMENTS,
  toPasoRequest,
  type Turno,
  TURNOS,
} from '../types/fijas';
import { useRutas, operacionApi } from '../../operacion';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { type Weekday, WEEKDAYS } from '../../../shared/utils/dates';
import { useSave } from '../../../shared/hooks/useSave';
import { optional, optionalNumber } from '../../../shared/utils/format';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox, Field } from '../../../shared/components/Form';
import { PasoEditor } from './PasoEditor';
import { Exceptions } from './Exceptions';
import { paradasFijasApi } from '../services/paradasFijasApi';

export function FijaForm({ fija, canEdit, onClose }: { fija: Fija | null; canEdit: boolean; onClose: () => void }) {
  const rutas = useRutas();
  const lugares = useQuery({ queryKey: keys.lugares, queryFn: () => operacionApi.listLugares() });
  const [f, setF] = useState({
    name: fija?.name ?? '',
    category: fija?.category ?? ('Convenio' as Category),
    kind: fija?.kind ?? ('Recoleccion' as Kind),
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
      mapsLink: p.lugarId || p.lat == null || p.lng == null ? '' : `${p.lat}, ${p.lng}`,
      kind: p.kind ?? '',
      turno: p.turno ?? '',
      before: p.before?.slice(0, 5) ?? '',
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
        category: f.category,
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
        pasos: pasos.map(toPasoRequest),
      };
      return fija ? paradasFijasApi.update(fija.id, body) : paradasFijasApi.create(body);
    },
    {
      refetch: [keys.paradasFijas, keys.agendaBoard],
      message: fija
        ? 'Parada fija guardada; sus paradas de los próximos días se actualizaron.'
        : 'Parada fija creada y agregada a los próximos días.',
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
            <input
              autoFocus
              value={f.name}
              onChange={(e) => change({ name: e.target.value })}
              required
              placeholder="Traspaso K, Insumos Florida, Convenio Wildress"
            />
          </Field>
          <Field label="Tipo de parada" hint={categoryOf(f.category).hint}>
            <select value={f.category} onChange={(e) => change({ category: e.target.value as Category })}>
              {CATEGORIES.map((k) => (
                <option key={k.key} value={k.key}>
                  {k.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Qué se hace">
            <select value={f.kind} onChange={(e) => change({ kind: e.target.value as Kind })}>
              {ACTIONS.map((k) => (
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
          Un paso es una parada fija; con varios pasos es una vuelta (un paso por tramo) y se visitan en orden. Cada paso ocupa un lugar del cupo.
        </p>
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
        {canEdit && (
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

        <h3 className="form-section">Detalle</h3>
        <div className="form-grid">
          <Field label={f.category === 'Traspaso' ? 'Qué se mueve' : 'Insumos o carga'} wide hint="Texto libre; no hay inventario.">
            <input
              value={f.carried}
              onChange={(e) => change({ carried: e.target.value })}
              placeholder={f.category === 'Traspaso' ? '2 galones de 10 L, 1 caja de ganchos' : undefined}
            />
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

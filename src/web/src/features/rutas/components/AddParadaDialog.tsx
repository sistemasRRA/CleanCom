import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import type { Kind, Plan } from '../types/rutas';
import { keys } from '../../../shared/api/queryKeys';
import { type Schemas } from '../../../shared/api/client';
import { ACTIONS, CATEGORIES, categoryOf, emptyPaso, PasoEditor, type PasoForm, PLACEMENTS, toPasoRequest, TURNOS } from '../../paradasFijas';
import { useSave } from '../../../shared/hooks/useSave';
import { optional } from '../../../shared/utils/format';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { rutasApi } from '../services/rutasApi';
import { operacionApi } from '../../operacion';

type AddKind = 'parada' | 'viaje';

/** "Agregar": parada de un día (one lugar) or viaje de un día (pasos in order), only for this date. */
export function AddDialog({ ruta, date, onClose }: { ruta: Plan; date: string; onClose: () => void }) {
  const lugares = useQuery({ queryKey: keys.lugares, queryFn: () => operacionApi.listLugares() });
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
      return type === 'viaje' ? rutasApi.addViaje(body) : rutasApi.addParada(body);
    },
    {
      refetch: [keys.rutasDia, keys.agendaBoard],
      message: type === 'viaje' ? 'Viaje agregado a la ruta.' : 'Parada agregada a la ruta.',
      onDone: onClose,
    },
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

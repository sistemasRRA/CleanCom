import { actionName, ACTIONS, type Kind, type Lugar, type PasoForm, type Turno, turnoName, TURNOS } from '../types/fijas';
import { Field } from '../../../shared/components/Form';
import { AddressLocation } from '../../../shared/components/AddressLocation';

/** "09:30:00" → "9:30 a.m." */
function ampm(time: string) {
  const [h, m] = time.split(':').map(Number);
  return `${h % 12 === 0 ? 12 : h % 12}:${String(m).padStart(2, '0')} ${h < 12 ? 'a.m.' : 'p.m.'}`;
}

export function PasoEditor({
  index,
  count,
  paso,
  lugares,
  defaultKind,
  defaultTurno,
  onChange,
  onMove,
  onRemove,
}: {
  index: number;
  count: number;
  paso: PasoForm;
  lugares: Lugar[];
  defaultKind: Kind;
  defaultTurno: Turno;
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
        <Field label="Indicación">
          <input value={paso.activity} onChange={(e) => onChange({ activity: e.target.value })} placeholder="Cargar en Leones, entregar insumos…" />
        </Field>
        {!paso.lugarId && (
          <>
            <Field label="Dirección" wide>
              <input value={paso.address} onChange={(e) => onChange({ address: e.target.value })} required />
            </Field>
            {/* The whole address goes as one text: the map search takes it as typed. */}
            <AddressLocation
              label="Link de Google Maps o coordenadas"
              placeholder="19.36, -99.19"
              hint="Mueve el pin o haz clic en el mapa donde está el lugar. También acepta el link completo de Google Maps. Para el mapa y el ruteo."
              query={{ street: paso.address }}
              canLocate={paso.address.trim().length >= 8}
              value={paso.mapsLink}
              onChange={(mapsLink) => onChange({ mapsLink })}
            />
          </>
        )}
        <Field label="Qué se hace en este paso">
          <select value={paso.kind} onChange={(e) => onChange({ kind: e.target.value as Kind | '' })}>
            <option value="">Igual que la parada ({actionName(defaultKind)})</option>
            {ACTIONS.map((a) => (
              <option key={a.key} value={a.key}>
                {a.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Turno de este paso">
          <select value={paso.turno} onChange={(e) => onChange({ turno: e.target.value as Turno | '' })}>
            <option value="">Igual que la parada ({turnoName(defaultTurno)})</option>
            {TURNOS.map((t) => (
              <option key={t.key} value={t.key}>
                {t.name}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Antes de" hint={paso.before ? `Antes: a las ${ampm(paso.before)}` : 'Hora límite para llegar; vacío = el fin de su turno.'}>
          <input type="time" value={paso.before} onChange={(e) => onChange({ before: e.target.value })} />
        </Field>
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

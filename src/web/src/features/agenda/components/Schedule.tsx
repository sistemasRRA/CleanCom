import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { type Schemas } from '../../../shared/api/client';
import type { Invitado } from '../../invitados';
import { useNotify } from '../../../shared/components/Toasts';
import { keys } from '../../../shared/api/queryKeys';
import { beneficiosKey, expiry, promocionesApi } from '../../promociones';
import { useSave } from '../../../shared/hooks/useSave';
import { optional, optionalNumber } from '../../../shared/utils/format';
import { QueryState } from '../../../shared/components/Page';
import { formatDate } from '../../../shared/utils/dates';
import { Checkbox, Field } from '../../../shared/components/Form';
import { agendaApi } from '../services/agendaApi';
import { operacionApi } from '../../operacion';

type Turno = Schemas['Turno'];

const TURNOS: { key: Turno; name: string }[] = [
  { key: 'CualquierHora', name: 'Cualquier hora' },
  { key: 'Manana', name: 'Mañana' },
  { key: 'Tarde', name: 'Tarde' },
  { key: 'HoraEspecifica', name: 'Hora específica' },
];

const SOURCES: Record<Schemas['ZonaSource'], string> = {
  Poligono: 'por ubicación',
  CodigoPostal: 'por código postal',
  Manual: 'asignada a mano',
  FueraDeCobertura: 'fuera de cobertura',
};

/** "2026-11-12T10:00" typed in Mexico City time (UTC-6 all year since 2022) as an ISO instant. */
function mexicoCityInstant(local: string) {
  return local ? `${local}:00-06:00` : null;
}

// ---- Schedule ----

export function Schedule({
  invitado,
  direccionId,
  canEdit,
  onScheduled,
}: {
  invitado: Invitado;
  direccionId: number;
  canEdit: boolean;
  onScheduled: () => void;
}) {
  const notify = useNotify();
  const [turno, setTurno] = useState<Turno>('CualquierHora');
  const [specificTime, setSpecificTime] = useState('');
  const [garments, setGarments] = useState('');
  const [lineas, setLineas] = useState<number[]>([]);
  const [notes, setNotes] = useState('');
  const [priority, setPriority] = useState(false);
  const [registeredAt, setRegisteredAt] = useState('');
  const [date, setDate] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [beneficioIds, setBeneficioIds] = useState<number[]>([]);

  const lineasServicio = useQuery({ queryKey: keys.lineasServicio, queryFn: () => operacionApi.listLineasServicio() });
  // Sellos and cupones the invitado can still use; the API checks línea, validity and combining.
  const beneficios = useQuery({
    queryKey: beneficiosKey(invitado.id),
    queryFn: () => promocionesApi.beneficiosOf(invitado.id),
  });
  const available = (beneficios.data ?? []).filter((b) => b.state === 'Disponible');
  const registered = mexicoCityInstant(registeredAt);
  const availability = useQuery({
    queryKey: ['agenda', direccionId, registered],
    queryFn: () =>
      agendaApi.availability({
        addressId: direccionId,
        lat: null,
        lng: null,
        postalCode: null,
        neighborhood: null,
        zonaId: null,
        registeredAt: registered,
      }),
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
      agendaApi.schedule({
        invitadoId: invitado.id,
        addressId: direccionId,
        turno,
        specificTime: turno === 'HoraEspecifica' ? `${specificTime}:00` : null,
        priority,
        garmentsApprox: optionalNumber(garments),
        driverNotes: optional(notes),
        lineaServicioIds: lineas,
        registeredAt: registered,
        date: date,
        beneficioIds,
      }),
    {
      refetch: [['agenda'], keys.pedidos, beneficiosKey(invitado.id)],
      onDone: (r) => {
        // Scheduled: close the modal and confirm with a toast. Not scheduled: keep it open with the engine's reason.
        if (r.scheduled) {
          notify(r.message);
          onScheduled();
          return;
        }
        setNotice(r.message);
        setDate(null);
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
        <QueryState inline loading={availability.isPending} error={availability.error}>
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
            {available.length > 0 && (
              <Field
                label="Aplicar beneficio"
                hint="Se aparta para este pedido y queda usado al cerrar la recolección. El descuento se aplica en el POS."
              >
                <div className="stack">
                  {available.map((b) => (
                    <Checkbox
                      key={b.id}
                      checked={beneficioIds.includes(b.id)}
                      onChange={(on) => setBeneficioIds(on ? [...beneficioIds, b.id] : beneficioIds.filter((x) => x !== b.id))}
                    >
                      {b.name} · {b.mechanic} <span className="muted">({expiry(b.expiresOn)})</span>
                    </Checkbox>
                  ))}
                </div>
              </Field>
            )}
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

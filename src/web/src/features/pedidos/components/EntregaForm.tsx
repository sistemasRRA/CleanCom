import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { type Schemas } from '../../../shared/api/client';
import { ENTREGA_TURNOS, type Turno } from '../types/pedidos';
import { useNotify } from '../../../shared/components/Toasts';
import { keys } from '../../../shared/api/queryKeys';
import { useSave } from '../../../shared/hooks/useSave';
import { refetch } from '../services/queryKeys';
import { QueryState } from '../../../shared/components/Page';
import { Field } from '../../../shared/components/Form';
import { pedidosApi } from '../services/pedidosApi';

/** The turno already agreed for the entrega, otherwise the recolección's (a specific time becomes cualquier hora). */
export function entregaTurno(paradas: Schemas['PedidoParadaDto'][]): Turno {
  const last = [...paradas].reverse();
  const turno = last.find((p) => p.kind === 'Entrega')?.turno ?? last.find((p) => p.kind === 'Recoleccion')?.turno;
  return !turno || turno === 'HoraEspecifica' ? 'CualquierHora' : turno;
}

export function EntregaForm({ id, turno: initialTurno, onDone }: { id: number; turno: Turno; onDone: () => void }) {
  const notify = useNotify();
  const days = useQuery({ queryKey: [...keys.pedidos, 'fechas-entrega', id], queryFn: () => pedidosApi.entregaDates(id) });
  const [date, setDate] = useState<string | null>(null);
  const [turno, setTurno] = useState<Turno>(initialTurno);
  const save = useSave(() => pedidosApi.scheduleEntrega(id, { date, turno }), {
    refetch,
    onDone: (r) => (r.scheduled ? (notify(r.message ?? 'Entrega programada.'), onDone()) : undefined),
  });
  return (
    <div className="inline-form">
      <QueryState inline loading={days.isPending} error={days.error}>
        {days.data && (
          <>
            <p className="muted">
              Zona {days.data.zonaCode} · Ruta {days.data.rutaCode}. Elige el turno y el día con cupo; la entrega ocupa un lugar como una recolección.
            </p>
            <Field label="Turno">
              <div className="segmented">
                {ENTREGA_TURNOS.map((t) => (
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
            <div className="entrega-days">
              {days.data.days.map((d) => (
                <button
                  key={d.date}
                  type="button"
                  className={`small${date === d.date ? ' selected' : ''}`}
                  disabled={!d.available}
                  onClick={() => setDate(d.date)}
                >
                  {d.label} · {d.occupied}/{d.cupo}
                </button>
              ))}
            </div>
          </>
        )}
      </QueryState>
      {save.data && !save.data.scheduled && <div className="alert">{save.data.message}</div>}
      {save.error && <div className="alert error">{save.error.message}</div>}
      <button type="button" className="primary small" disabled={save.isPending || !date} onClick={() => save.mutate(undefined)}>
        Programar entrega
      </button>
    </div>
  );
}

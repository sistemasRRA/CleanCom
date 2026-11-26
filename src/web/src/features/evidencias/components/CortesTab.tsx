import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { QueryState } from '../../../shared/components/Page';
import { useSession } from '../../../shared/session/Session';
import { addDays, clock, formatDateTime, todayInMexicoCity } from '../../../shared/utils/dates';
import { evidenciasApi } from '../services/evidenciasApi';
import { cortesKey } from '../services/queryKeys';
import { type Corte, CORTE_STATE, difference, METHOD_NAME, money } from '../types/evidencias';
import { CerrarCorte } from './CerrarCorte';
import { CerrarTurnoOficina } from './CerrarTurnoOficina';
import { ReabrirCorte } from './ReabrirCorte';

const km = (n?: number | null) => (n == null ? '—' : `${n.toLocaleString('es-MX')} km`);

/**
 * Corte de caja of a day (GET /cortes): per ruta, what the chofer recorded in the visits, what they declared at the end
 * of the shift and the POS, with the differences the API calculates. A declared corte is reviewed and closed.
 */
export function CortesTab() {
  const { can } = useSession();
  const canEdit = can('evidencias-y-caja', 'edit');
  // Special permissions of the role (Usuarios › Roles y permisos).
  const canReopen = can('reabrir-cortes', 'edit');
  const canCloseShift = can('cerrar-turnos', 'edit');
  const [fecha, setFecha] = useState(todayInMexicoCity);
  const [closing, setClosing] = useState<Corte | null>(null);
  const [reopening, setReopening] = useState<Corte | null>(null);
  const [closingShift, setClosingShift] = useState<Corte | null>(null);
  const day = useQuery({ queryKey: [...cortesKey, fecha], queryFn: () => evidenciasApi.cortes({ fecha }) });
  const d = day.data;

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div className="actions">
            <button type="button" className="small" onClick={() => setFecha(addDays(fecha, -1))} aria-label="Día anterior">
              ‹
            </button>
            <input type="date" value={fecha} onChange={(e) => e.target.value && setFecha(e.target.value)} style={{ width: 'auto' }} />
            <button type="button" className="small" onClick={() => setFecha(addDays(fecha, 1))} aria-label="Día siguiente">
              ›
            </button>
            {fecha !== todayInMexicoCity() && (
              <button type="button" className="small" onClick={() => setFecha(todayInMexicoCity())}>
                Hoy
              </button>
            )}
          </div>
          {d && (
            <span className="muted">
              <strong className="caja-label">{d.label}</strong>
              {d.porRevisar > 0 && ` · ${d.porRevisar} por revisar`}
            </span>
          )}
        </div>
        <p className="muted caja-hint">
          El cobro oficial está en el punto de venta; lo que anota el chofer es referencia. Una diferencia negativa es dinero que falta.
        </p>
      </section>

      <QueryState loading={day.isPending} error={day.error}>
        {d?.cortes.length === 0 && <div className="card empty">Ninguna ruta trabajó este día.</div>}
        <div className="caja-cortes">
          {d?.cortes.map((c) => (
            <section key={c.rutaDiaId} className="card caja-corte">
              <div className="card__header">
                <div>
                  <h2>
                    <span className="ruta-dot" style={{ background: c.rutaColor }} /> Ruta {c.rutaCode}{' '}
                    <span className={`badge ${CORTE_STATE[c.state].tone}`}>{CORTE_STATE[c.state].name}</span>
                    {c.hasDifference && c.state !== 'TurnoAbierto' && <span className="badge danger">Con diferencias</span>}
                  </h2>
                  <p className="muted">
                    {c.chofer ?? 'Sin chofer'}
                    {c.unidad && ` · ${c.unidad}`}
                    {c.shiftStartedAt && ` · turno de ${clock(c.shiftStartedAt)}`}
                    {c.shiftEndedAt && ` a ${clock(c.shiftEndedAt)}`}
                    {c.startKm != null && ` · ${km(c.startKm)}`}
                    {c.endKm != null && c.startKm != null && ` a ${km(c.endKm)} (${km(c.endKm - c.startKm)})`}
                  </p>
                </div>
                {canEdit && c.state === 'PorRevisar' && (
                  <button type="button" className="primary" onClick={() => setClosing(c)}>
                    Revisar y cerrar
                  </button>
                )}
              </div>

              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Forma de pago</th>
                      <th className="end">En visitas</th>
                      <th className="end">Declarado</th>
                      <th className="end">Punto de venta</th>
                      <th className="end">Dif. vs visitas</th>
                      <th className="end">Dif. vs punto de venta</th>
                    </tr>
                  </thead>
                  <tbody>
                    {c.lines.map((l) => (
                      <tr key={l.method}>
                        <td>{METHOD_NAME[l.method]}</td>
                        <td className="end">
                          {money(l.registered)}
                          {l.registeredCount > 0 && <div className="muted">{l.registeredCount} cobros</div>}
                        </td>
                        <td className="end">{money(l.declared)}</td>
                        <td className="end">{money(l.pos)}</td>
                        <td className={`end${l.vsRegistered ? ' caja-diff' : ''}`}>{difference(l.vsRegistered)}</td>
                        <td className={`end${l.vsPos ? ' caja-diff' : ''}`}>{difference(l.vsPos)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr>
                      <th>Total</th>
                      <th className="end">{money(c.registered)}</th>
                      <th className="end">{money(c.declared)}</th>
                      <th colSpan={3}></th>
                    </tr>
                  </tfoot>
                </table>
              </div>

              {c.state === 'TurnoAbierto' && <p className="muted">El chofer declara lo cobrado al cerrar su turno.</p>}
              {c.state === 'SinTurno' && <p className="muted">El chofer no inició turno este día.</p>}
              {c.shiftClosedBy && (
                <p className="muted">
                  Turno cerrado desde oficina por {c.shiftClosedBy}
                  {c.shiftEndedAt && ` a las ${clock(c.shiftEndedAt)}`}.
                </p>
              )}
              {c.choferNotes && (
                <p>
                  <strong>{c.shiftClosedBy ? 'Nota de oficina al cerrar el turno:' : 'Nota del chofer:'}</strong> {c.choferNotes}
                </p>
              )}
              {c.reopenedAt && (
                <p className="muted">
                  Reabierto por {c.reopenedBy ?? '—'} el {formatDateTime(c.reopenedAt)}: {c.reopenReason}
                </p>
              )}
              {c.state === 'Cerrado' && (
                <p className="caja-closed">
                  <strong>Folio {c.folio}</strong> · cerrado por {c.closedBy ?? '—'} el {formatDateTime(c.closedAt)}
                  {c.officeNotes && (
                    <>
                      <br />
                      {c.officeNotes}
                    </>
                  )}
                </p>
              )}
              {c.state === 'TurnoAbierto' && canCloseShift && (
                <div className="actions">
                  <button type="button" onClick={() => setClosingShift(c)}>
                    Cerrar turno por el chofer
                  </button>
                </div>
              )}
              {c.state === 'Cerrado' && canReopen && (
                <div className="actions">
                  <button type="button" className="small" onClick={() => setReopening(c)}>
                    Reabrir corte
                  </button>
                </div>
              )}
            </section>
          ))}
        </div>
      </QueryState>
      {closing && <CerrarCorte corte={closing} onClose={() => setClosing(null)} />}
      {reopening && <ReabrirCorte corte={reopening} onClose={() => setReopening(null)} />}
      {closingShift && <CerrarTurnoOficina corte={closingShift} onClose={() => setClosingShift(null)} />}
    </>
  );
}

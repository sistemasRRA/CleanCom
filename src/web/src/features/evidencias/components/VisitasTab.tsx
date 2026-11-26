import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useRutas } from '../../operacion';
import { EvidenciaThumb } from '../../../shared/components/EvidenciaThumb';
import { Pagination } from '../../../shared/components/Pagination';
import { QueryState } from '../../../shared/components/Page';
import { useNotify } from '../../../shared/components/Toasts';
import { clock, shortDay, todayInMexicoCity } from '../../../shared/utils/dates';
import { evidenciasApi } from '../services/evidenciasApi';
import { visitasKey } from '../services/queryKeys';
import { KIND, METHOD_NAME, METHODS, type Method, money } from '../types/evidencias';
import { GaleriaPedido } from './GaleriaPedido';

const PAGE_SIZE = 25;

/**
 * Visits of a period (GET /visitas): each closed visit with its photos (marked when taken outside the radius),
 * signature, tickets and reference payment, the totals of the filters and the Excel export.
 */
export function VisitasTab() {
  const notify = useNotify();
  const rutas = useRutas();
  const [desde, setDesde] = useState(todayInMexicoCity);
  const [hasta, setHasta] = useState(todayInMexicoCity);
  const [rutaId, setRutaId] = useState<number | null>(null);
  const [formaPago, setFormaPago] = useState<Method | null>(null);
  const [pagina, setPagina] = useState(1);
  const [galeria, setGaleria] = useState<number | null>(null);
  const [exporting, setExporting] = useState(false);
  const filters = { desde, hasta, ruta: rutaId ?? undefined, formaPago: formaPago ?? undefined };
  const list = useQuery({
    queryKey: [...visitasKey, filters, pagina],
    queryFn: () => evidenciasApi.visitas({ ...filters, pagina, porPagina: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });
  const d = list.data;

  /** Changing a filter goes back to the first page. */
  function filter(change: () => void) {
    change();
    setPagina(1);
  }

  async function exportar() {
    setExporting(true);
    try {
      const blob = (await evidenciasApi.exportar(filters)) as Blob;
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = desde === hasta ? `visitas-${desde}.xlsx` : `visitas-${desde}-a-${hasta}.xlsx`;
      link.click();
      URL.revokeObjectURL(link.href);
    } catch (e) {
      notify((e as Error).message, true);
    } finally {
      setExporting(false);
    }
  }

  return (
    <>
      <section className="card">
        <div className="card__header">
          <div className="actions">
            <label className="caja-field">
              Desde
              <input type="date" value={desde} max={hasta} onChange={(e) => e.target.value && filter(() => setDesde(e.target.value))} />
            </label>
            <label className="caja-field">
              Hasta
              <input type="date" value={hasta} min={desde} onChange={(e) => e.target.value && filter(() => setHasta(e.target.value))} />
            </label>
            <select value={rutaId ?? ''} onChange={(e) => filter(() => setRutaId(e.target.value ? Number(e.target.value) : null))} style={{ width: 'auto' }}>
              <option value="">Todas las rutas</option>
              {rutas.data
                ?.filter((r) => r.active)
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    Ruta {r.code}
                  </option>
                ))}
            </select>
            <select value={formaPago ?? ''} onChange={(e) => filter(() => setFormaPago((e.target.value as Method) || null))} style={{ width: 'auto' }}>
              <option value="">Toda forma de pago</option>
              {METHODS.map((m) => (
                <option key={m.key} value={m.key}>
                  {m.name}
                </option>
              ))}
            </select>
          </div>
          <button type="button" disabled={exporting || !d || d.rowCount === 0} onClick={() => void exportar()}>
            {exporting ? 'Exportando…' : 'Exportar a Excel'}
          </button>
        </div>

        {d && (
          <div className="caja-totals">
            <div>
              <strong>{d.totals.done}</strong>
              <span className="muted">realizadas</span>
            </div>
            <div>
              <strong>{d.totals.notDone}</strong>
              <span className="muted">no realizadas</span>
            </div>
            <div>
              <strong>{money(d.totals.collected)}</strong>
              <span className="muted">cobrado (referencia)</span>
            </div>
            {d.totals.byMethod.map((m) => (
              <div key={m.method}>
                <strong>{money(m.amount)}</strong>
                <span className="muted">
                  {METHOD_NAME[m.method]} · {m.count}
                </span>
              </div>
            ))}
            <div className={d.totals.outsideRadius > 0 ? 'warn' : ''}>
              <strong>{d.totals.outsideRadius}</strong>
              <span className="muted">fuera de radio (más de {d.toleranceMeters} m)</span>
            </div>
          </div>
        )}

        <QueryState loading={list.isPending} error={list.error}>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Ruta</th>
                  <th>Visita</th>
                  <th>Ticket</th>
                  <th>Pago</th>
                  <th>Evidencia</th>
                  <th>Notas</th>
                </tr>
              </thead>
              <tbody>
                {d?.rows.map((v) => (
                  <tr key={v.paradaId} className={v.state === 'NoRealizada' ? 'muted-row' : undefined}>
                    <td>
                      {shortDay(v.date)}
                      <div className="muted">{clock(v.closedAt)}</div>
                    </td>
                    <td>
                      <span className="ruta-dot" style={{ background: v.rutaColor }} /> {v.rutaCode}
                      {v.chofer && <div className="muted">{v.chofer}</div>}
                    </td>
                    <td>
                      <strong>{v.name}</strong>
                      <div className="muted">
                        {KIND[v.kind]}
                        {v.garments != null && ` · ${v.garments} prendas`}
                        {v.pedidoId != null && (
                          <>
                            {' · '}
                            <button type="button" className="link" onClick={() => setGaleria(v.pedidoId!)}>
                              Pedido {v.pedidoId}
                            </button>
                          </>
                        )}
                      </div>
                      {v.state === 'NoRealizada' && <span className="badge danger">No realizada</span>}
                    </td>
                    <td>{v.tickets.length > 0 ? v.tickets.join(', ') : <span className="muted">—</span>}</td>
                    <td>
                      {v.paymentMethod ? (
                        <>
                          <strong>{money(v.paymentAmount)}</strong>
                          <div className="muted">{METHOD_NAME[v.paymentMethod]}</div>
                        </>
                      ) : (
                        <span className="muted">—</span>
                      )}
                    </td>
                    <td>
                      <div className="evidencias">
                        {v.photos.map((p) => (
                          <EvidenciaThumb key={p.id} id={p.id} kind="Foto" distance={p.distanceMeters} outside={p.outsideRadius} small />
                        ))}
                        {v.signatureId != null && <EvidenciaThumb id={v.signatureId} kind="Firma" small />}
                        {v.photos.length === 0 && v.signatureId == null && <span className="badge warning">Sin foto</span>}
                      </div>
                      {v.arrivedOutsideRadius && <span className="badge danger">Llegada fuera de radio</span>}
                    </td>
                    <td className="caja-notes">{v.reason ?? v.notes ?? <span className="muted">—</span>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {d?.rows.length === 0 && <div className="empty">Sin visitas cerradas con estos filtros.</div>}
          </div>
          {d && (
            <Pagination
              page={d.page}
              pageCount={Math.max(1, Math.ceil(d.rowCount / d.pageSize))}
              total={d.rowCount}
              pageSize={d.pageSize}
              setPage={setPagina}
            />
          )}
        </QueryState>
      </section>
      {galeria != null && <GaleriaPedido pedidoId={galeria} onClose={() => setGaleria(null)} />}
    </>
  );
}

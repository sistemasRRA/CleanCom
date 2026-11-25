import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import { type Row, type Status, STATUS_ORDER } from '../types/crm';
import { useRutas } from '../../operacion';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { PAGE_SIZE, Pagination } from '../../../shared/components/Pagination';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { InvitadoForm, invitadosApi } from '../../invitados';
import { StatusBadge } from '../components/StatusBadge';
import { shortDay } from '../../../shared/utils/dates';
import { ContactForm } from '../components/ContactForm';
import { Ficha } from '../components/FichaCliente';

/**
 * Clientes (CRM): status of each invitado, "Llamar hoy", recording contacts and the ficha
 * (docs/pantallas/oficina/clientes-crm.md). Status and lists come calculated from the API.
 */
export function Clientes() {
  const canEdit = useSession().can('clientes-crm', 'edit');
  const [tab, setTab] = useState<'llamar' | 'todos'>('llamar');
  const [status, setStatus] = useState<Status | null>(null);
  const [rutaId, setRutaId] = useState<number | null>(null);
  const [text, setText] = useState('');
  const [q, setQ] = useState('');
  const [contacting, setContacting] = useState<Row | null>(null);
  // Other screens open a ficha with ?invitado= (Pedidos › Ver en CRM).
  const [params] = useSearchParams();
  const [ficha, setFicha] = useState<number | null>(() => Number(params.get('invitado')) || null);
  // Capturing an invitado is an Agendar permission in the API (POST /invitados).
  const canCapture = useSession().can('agendar', 'edit');
  const [capturing, setCapturing] = useState(false);
  const rutas = useRutas();

  useEffect(() => {
    const t = setTimeout(() => setQ(text.trim()), 300);
    return () => clearTimeout(t);
  }, [text]);

  // Paged in the API (there can be thousands of invitados); a new tab, filter or search starts at page 1.
  const [page, setPage] = useState(1);
  useEffect(() => setPage(1), [tab, status, rutaId, q]);

  const list = useQuery({
    queryKey: [...keys.crm, tab, status, rutaId, q, page],
    queryFn: () =>
      tab === 'llamar'
        ? invitadosApi.callToday({ ruta: rutaId ?? undefined, q, pagina: page, porPagina: PAGE_SIZE })
        : invitadosApi.list({ estatus: status ?? undefined, ruta: rutaId ?? undefined, q, pagina: page, porPagina: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  });
  const d = list.data;

  return (
    <div className="stack">
      <PageHeader
        title="Clientes (CRM)"
        description="Estatus de cada invitado por su frecuencia real y la lista de a quién llamar un día antes de que pase su ruta."
      >
        {canCapture && (
          <button type="button" className="primary" onClick={() => setCapturing(true)}>
            Nuevo cliente
          </button>
        )}
      </PageHeader>
      {/* Captured here; then its ficha opens, with "Agendar" if it already wants a recolección. */}
      {capturing && (
        <InvitadoForm
          onClose={() => setCapturing(false)}
          onSaved={(i) => {
            setCapturing(false);
            setFicha(i.id);
          }}
        />
      )}

      {d && (
        <div className="crm-counts">
          {STATUS_ORDER.map((s) => {
            const count = d.counts.find((c) => c.status === s)?.count ?? 0;
            return (
              <button
                key={s}
                type="button"
                className={`crm-count${status === s && tab === 'todos' ? ' selected' : ''}`}
                onClick={() => {
                  setTab('todos');
                  setStatus(status === s ? null : s);
                }}
              >
                <strong>{count}</strong>
                <StatusBadge status={s} />
              </button>
            );
          })}
          <span className="muted">de {d.total} invitados (sin tiendas)</span>
        </div>
      )}

      <section className="card">
        <div className="card__header">
          <div className="segmented">
            <button type="button" className={`segmented__option${tab === 'llamar' ? ' selected' : ''}`} onClick={() => setTab('llamar')}>
              Llamar hoy{d ? ` (${d.callTodayCount})` : ''}
            </button>
            <button
              type="button"
              className={`segmented__option${tab === 'todos' ? ' selected' : ''}`}
              onClick={() => {
                setTab('todos');
                setStatus(null);
              }}
            >
              Todos
            </button>
          </div>
          <div className="actions">
            <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Nombre, colonia o teléfono" style={{ maxWidth: 240 }} />
            <select value={rutaId ?? ''} onChange={(e) => setRutaId(e.target.value ? Number(e.target.value) : null)} style={{ width: 'auto' }}>
              <option value="">Todas las rutas</option>
              {rutas.data
                ?.filter((r) => r.active)
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    Ruta {r.code}
                  </option>
                ))}
            </select>
          </div>
        </div>
        {tab === 'llamar' && (
          <p className="muted crm-hint">Le toca o en riesgo, cuya ruta pasa mañana por su zona; sin fecha de contacto futura y sin "no contactar".</p>
        )}
        <QueryState loading={list.isPending} error={list.error}>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Invitado</th>
                  <th>Zona</th>
                  <th>Estatus</th>
                  <th>Por qué</th>
                  <th>Visita sugerida</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {d?.rows.map((r) => (
                  <tr key={r.id}>
                    <td>
                      <button type="button" className="link" onClick={() => setFicha(r.id)}>
                        <strong>{r.name}</strong>
                      </button>
                      <div className="muted">
                        {r.whatsAppPhone}
                        {r.neighborhood && ` · ${r.neighborhood}`}
                      </div>
                    </td>
                    <td>
                      {r.zonaCode ?? <span className="badge warning">Sin zona</span>}
                      {r.rutaCode && <div className="muted">Ruta {r.rutaCode}</div>}
                    </td>
                    <td>
                      <StatusBadge status={r.status} manual={r.statusIsManual} />
                      {r.doNotContact && <div className="badge danger crm-flag">No contactar</div>}
                    </td>
                    <td className="crm-why">
                      {r.why}
                      {r.nextRecoleccion && <div className="muted">Ya tiene recolección el {shortDay(r.nextRecoleccion)}</div>}
                      {r.nextContactOn && <div className="muted">Volver a contactar: {shortDay(r.nextContactOn)}</div>}
                    </td>
                    <td>{shortDay(r.suggestedVisit)}</td>
                    <td className="end">
                      <div className="actions tight end">
                        <a className="button small" href={`https://wa.me/52${r.whatsAppPhone}`} target="_blank" rel="noreferrer">
                          WhatsApp
                        </a>
                        {canEdit && (
                          <button type="button" className="small" onClick={() => setContacting(r)}>
                            Registrar
                          </button>
                        )}
                        <Link className="button small" to={`/agendar?invitado=${r.id}`}>
                          Agendar
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {d?.rows.length === 0 && <div className="empty">{tab === 'llamar' ? 'Nadie por llamar hoy.' : 'Sin invitados con estos filtros.'}</div>}
          </div>
          {d && (
            <Pagination
              page={d.page}
              pageCount={Math.max(1, Math.ceil(d.rowCount / d.pageSize))}
              total={d.rowCount}
              pageSize={d.pageSize}
              setPage={setPage}
            />
          )}
        </QueryState>
      </section>

      {contacting && <ContactForm invitadoId={contacting.id} name={contacting.name} onClose={() => setContacting(null)} />}
      {ficha !== null && <Ficha id={ficha} onClose={() => setFicha(null)} onContact={(r) => setContacting(r)} />}
    </div>
  );
}

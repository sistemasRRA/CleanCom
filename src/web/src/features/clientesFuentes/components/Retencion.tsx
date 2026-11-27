import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { QueryState } from '../../../shared/components/Page';
import { clientesFuentesApi, clientesFuentesKey } from '../services/clientesFuentesApi';
import { percent, type Tipo } from '../types/clientesFuentes';

/**
 * Retención por generación (GET /gerencia/clientes/retencion): of the nuevos of each of the last 6 months, how many
 * ordered again within 30, 60 and 90 days. The share is over those whose days already passed.
 */
export function Retencion({ fecha, tipo }: { fecha: string; tipo: Tipo }) {
  const [fuente, setFuente] = useState('');
  const query = useQuery({
    queryKey: [...clientesFuentesKey, 'retencion', fecha, tipo, fuente],
    queryFn: () => clientesFuentesApi.retention({ fecha, tipo, fuente: fuente || undefined }),
  });
  const d = query.data;

  const cell = (repeated: number, base: number, pct: number | null | undefined, nuevos: number) =>
    base === 0 ? (
      <span className="muted">{nuevos === 0 ? '—' : 'En curso'}</span>
    ) : (
      <>
        <strong>{percent(pct)}</strong>
        <div className="muted">
          {repeated} de {base}
          {base < nuevos && ' (en curso)'}
        </div>
      </>
    );

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Retención por generación</h2>
          <p className="muted">De los nuevos de cada mes, cuántos volvieron a pedir a los 30, 60 y 90 días de su primer pedido.</p>
        </div>
        <select value={fuente} onChange={(e) => setFuente(e.target.value)} style={{ width: 'auto' }} aria-label="Fuente">
          <option value="">Todas las fuentes</option>
          {d?.fuentes.map((f) => (
            <option key={f} value={f}>
              {f}
            </option>
          ))}
        </select>
      </div>
      <QueryState loading={query.isPending} error={query.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Mes del primer pedido</th>
                <th className="end">Nuevos</th>
                <th className="end">A 30 días</th>
                <th className="end">A 60 días</th>
                <th className="end">A 90 días</th>
              </tr>
            </thead>
            <tbody>
              {d?.cohorts.map((c) => (
                <tr key={c.month}>
                  <td>{c.label}</td>
                  <td className="end">{c.nuevos}</td>
                  <td className="end">{cell(c.repitieron30, c.base30, c.pct30, c.nuevos)}</td>
                  <td className="end">{cell(c.repitieron60, c.base60, c.pct60, c.nuevos)}</td>
                  <td className="end">{cell(c.repitieron90, c.base90, c.pct90, c.nuevos)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </QueryState>
    </section>
  );
}

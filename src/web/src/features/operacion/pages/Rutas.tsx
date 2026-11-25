import { useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { useRutas } from '../services/queries';
import { type Ruta, shortTime } from '../types/rutasZonas';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { RutaForm } from '../components/RutaForm';

// ---------------- Rutas ----------------

export function Rutas() {
  const canEdit = useSession().can('operacion', 'edit');
  const rutas = useRutas();
  const [editing, setEditing] = useState<Ruta | 'new' | null>(null);

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Rutas</h2>
          <p className="muted">Vacío en cupo, meta o regreso = usa la regla general de Operación.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva ruta
          </button>
        )}
      </div>
      <QueryState loading={rutas.isPending} error={rutas.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Código</th>
                <th>Nombre</th>
                <th>Cupo</th>
                <th>Meta</th>
                <th>Regreso</th>
                <th>Regreso sábado</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rutas.data?.map((r) => (
                <tr key={r.id} className={r.active ? '' : 'inactive'}>
                  <td>
                    <span className="ruta-color" style={{ background: r.color }} />
                    <strong>{r.code}</strong>
                  </td>
                  <td>{r.name}</td>
                  <td className="num">{r.cupoParadas ?? <span className="muted">general</span>}</td>
                  <td className="num">{r.recoleccionGoal ?? <span className="muted">general</span>}</td>
                  <td className="num">{shortTime(r.maxReturnTime) ?? <span className="muted">general</span>}</td>
                  <td className="num">{shortTime(r.maxReturnTimeSaturday) ?? <span className="muted">general</span>}</td>
                  <td>
                    <ActiveBadge active={r.active} />
                  </td>
                  <td>
                    {canEdit && (
                      <button type="button" className="small" onClick={() => setEditing(r)}>
                        Editar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {rutas.data?.length === 0 && <div className="empty">Todavía no hay rutas.</div>}
        </div>
      </QueryState>
      {editing && <RutaForm ruta={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

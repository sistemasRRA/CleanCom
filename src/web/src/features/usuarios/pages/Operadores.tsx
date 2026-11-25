import { useState } from 'react';
import { useSession } from '../../../shared/session/Session';
import { useOperadores } from '../services/queries';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { useRutas } from '../../operacion';
import type { Operador } from '../types/users';
import { ActiveBadge, QueryState } from '../../../shared/components/Page';
import { OperadorForm } from '../components/OperadorForm';

// ---------------- Operadores ----------------

export function Operadores() {
  const canEdit = useSession().can('usuarios', 'edit');
  const operadores = useOperadores();
  const pagedOperadores = usePaged(operadores.data);
  const rutas = useRutas();
  const [editing, setEditing] = useState<Operador | 'new' | null>(null);
  const findRuta = (id?: number | null) => rutas.data?.find((r) => r.id === id);

  return (
    <section className="card">
      <div className="card__header">
        <div>
          <h2>Operadores</h2>
          <p className="muted">Choferes de la nómina. Pueden existir sin usuario en la app.</p>
        </div>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nuevo operador
          </button>
        )}
      </div>
      <QueryState loading={operadores.isPending} error={operadores.error}>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Teléfono</th>
                <th>Ruta habitual</th>
                <th>Usuario</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pagedOperadores.rows.map((o) => {
                const ruta = findRuta(o.usualRutaId);
                return (
                  <tr key={o.id} className={o.active ? '' : 'inactive'}>
                    <td>
                      <strong>{o.name}</strong>
                    </td>
                    <td>{o.phone ?? <span className="muted">—</span>}</td>
                    <td>
                      {ruta ? (
                        <>
                          <span className="ruta-color" style={{ background: ruta.color }} />
                          {ruta.code}
                        </>
                      ) : (
                        <span className="muted">—</span>
                      )}
                    </td>
                    <td>{o.userId ? <span className="badge brand">con usuario</span> : <span className="muted">sin usuario</span>}</td>
                    <td>
                      <ActiveBadge active={o.active} />
                    </td>
                    <td className="end">
                      {canEdit && (
                        <button type="button" className="small" onClick={() => setEditing(o)}>
                          Editar
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {operadores.data?.length === 0 && <div className="empty">Sin operadores.</div>}
        </div>
        <Pagination {...pagedOperadores} />
      </QueryState>
      {editing && <OperadorForm operador={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </section>
  );
}

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import { actionName, CATEGORIES, type Category, categoryOf, dayShort, type Fija, placementName, turnoName } from '../types/fijas';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { Pagination, usePaged } from '../../../shared/components/Pagination';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { WeekGrid } from '../components/WeekGrid';
import { Checkbox } from '../../../shared/components/Form';
import { WEEKDAYS } from '../../../shared/utils/dates';
import { FijaState } from '../components/FijaState';
import { FijaForm } from '../components/FijaForm';
import { paradasFijasApi } from '../services/paradasFijasApi';

/**
 * Paradas fijas (docs/pantallas/oficina/paradas-fijas.md): convenios, insumos and traspasos that repeat every week.
 * A vuelta fija is one record with several pasos. The API generates them every night for the days ahead and they take cupo.
 */
export function ParadasFijas() {
  const canEdit = useSession().can('paradas-fijas', 'edit');
  const [category, setCategory] = useState<Category | ''>('');
  const [inactive, setInactive] = useState(false);
  const [editing, setEditing] = useState<Fija | 'new' | null>(null);

  const list = useQuery({
    queryKey: [...keys.paradasFijas, 'lista', category, inactive],
    queryFn: () => paradasFijasApi.list({ tipo: category || undefined, inactivas: inactive }),
    placeholderData: (previous) => previous,
  });
  const open = async (id: number) => setEditing(await paradasFijasApi.get(id));

  const pagedList = usePaged(list.data, `${category}|${inactive}`);
  return (
    <div className="stack">
      <PageHeader
        title="Paradas fijas"
        description="El trabajo que se repite cada semana. Se generan solas cada noche para los próximos días y cuentan dentro del cupo de cada ruta."
      >
        {canEdit && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva parada fija
          </button>
        )}
      </PageHeader>

      <WeekGrid onOpen={open} canEdit={canEdit} />

      <section className="card">
        <div className="card__header">
          <h2>Todas las paradas fijas</h2>
          <div className="actions">
            <select value={category} onChange={(e) => setCategory(e.target.value as Category | '')} style={{ width: 'auto' }}>
              <option value="">Todos los tipos</option>
              {CATEGORIES.map((k) => (
                <option key={k.key} value={k.key}>
                  {k.name}
                </option>
              ))}
            </select>
            <Checkbox checked={inactive} onChange={setInactive}>
              Ver inactivas
            </Checkbox>
          </div>
        </div>
        <QueryState loading={list.isPending} error={list.error}>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Parada</th>
                  <th>Tipo</th>
                  <th>Ruta</th>
                  <th>Días</th>
                  <th>Turno</th>
                  <th>Lugar en la ruta</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {pagedList.rows.map((f) => (
                  <tr key={f.id} className={f.active ? '' : 'inactive'}>
                    <td>
                      <strong>{f.name}</strong>
                      <div className="muted fija-pasos">{f.pasos.map((p) => p.lugarName ?? p.address).join(' → ')}</div>
                    </td>
                    <td>
                      <span className={`badge ${categoryOf(f.category).tone}`}>{categoryOf(f.category).name}</span>
                      <div className="muted">
                        {actionName(f.kind)}
                        {f.isVuelta && ` · vuelta de ${f.pasos.length} pasos`}
                      </div>
                    </td>
                    <td>
                      <span className="ruta-color" style={{ background: f.rutaColor }} />
                      {f.rutaCode}
                    </td>
                    <td className="muted">
                      {WEEKDAYS.filter((d) => f.days.includes(d.key))
                        .map((d) => dayShort(d.key))
                        .join(' ')}
                    </td>
                    <td>{turnoName(f.turno)}</td>
                    <td>{placementName(f.placement)}</td>
                    <td>
                      <FijaState fija={f} />
                    </td>
                    <td className="end">
                      <button type="button" className="small" onClick={() => open(f.id)}>
                        {canEdit ? 'Editar' : 'Ver'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {list.data?.length === 0 && <div className="empty">Sin paradas fijas con estos filtros.</div>}
          </div>
          <Pagination {...pagedList} />
        </QueryState>
      </section>

      {editing && <FijaForm fija={editing === 'new' ? null : editing} canEdit={canEdit} onClose={() => setEditing(null)} />}
    </div>
  );
}

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import { listKey } from '../services/queryKeys';
import '../../../shared/api/client';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { Checkbox } from '../../../shared/components/Form';
import { UnidadCard } from '../components/UnidadCard';
import { UnidadDialog } from '../components/UnidadDialog';
import { UnidadForm } from '../components/UnidadForm';
import { RulesDialog } from '../components/RulesDialog';
import { unidadesApi } from '../services/unidadesApi';

/**
 * Unidades (docs/pantallas/oficina/unidades.md): one card per vehicle with its notices, service and the year's spending.
 * The notices, the next service and the costs come computed from the API; the screen only shows and sends data.
 */
export function Unidades() {
  const canEdit = useSession().can('unidades', 'edit');
  const [includeInactive, setIncludeInactive] = useState(false);
  const [open, setOpen] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [rules, setRules] = useState(false);
  const list = useQuery({
    queryKey: [...listKey, includeInactive],
    queryFn: () => unidadesApi.list({ includeInactive }),
  });

  return (
    <>
      <PageHeader title="Unidades" description="Verificación, seguro, tarjeta de circulación, servicios, fallas y pagos de cada unidad.">
        <Checkbox checked={includeInactive} onChange={setIncludeInactive}>
          Ver dadas de baja
        </Checkbox>
        <button type="button" onClick={() => setRules(true)}>
          Reglas de aviso
        </button>
        {canEdit && (
          <button type="button" className="primary" onClick={() => setCreating(true)}>
            Nueva unidad
          </button>
        )}
      </PageHeader>

      <QueryState loading={list.isLoading} error={list.error}>
        {list.data && (
          <>
            {list.data.withPending > 0 && (
              <div className="alert">
                {list.data.withPending} unidad{list.data.withPending === 1 ? '' : 'es'} con pendientes.
              </div>
            )}
            {list.data.unidades.length === 0 ? (
              <div className="empty">No hay unidades registradas.{canEdit && ' Agrega la primera con "Nueva unidad".'}</div>
            ) : (
              <div className="unidad-cards">
                {list.data.unidades.map((u) => (
                  <UnidadCard key={u.id} unidad={u} onOpen={() => setOpen(u.id)} />
                ))}
              </div>
            )}
          </>
        )}
      </QueryState>

      {open !== null && <UnidadDialog id={open} canEdit={canEdit} onClose={() => setOpen(null)} />}
      {creating && <UnidadForm onClose={() => setCreating(false)} onSaved={(d) => (setCreating(false), setOpen(d.card.id))} />}
      {rules && <RulesDialog canEdit={canEdit} onClose={() => setRules(false)} />}
    </>
  );
}

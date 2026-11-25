import { type ReactNode, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { type Schemas } from '../../../shared/api/client';
import { useSession } from '../../../shared/session/Session';
import { useNotify } from '../../../shared/components/Toasts';
import { keys } from '../../../shared/api/queryKeys';
import { useSave } from '../../../shared/hooks/useSave';
import { promocionesApi } from '../services/promocionesApi';

type Beneficio = Schemas['BeneficioDto'];

type State = Schemas['BeneficioState'];

export const beneficiosKey = (invitadoId: number) => ['beneficios', invitadoId] as const;

const STATE: Record<State, { text: string; tone: string }> = {
  Disponible: { text: 'Disponible', tone: 'success' },
  Apartado: { text: 'Apartado', tone: 'brand' },
  Usado: { text: 'Usado', tone: '' },
  Vencido: { text: 'Vencido', tone: 'warning' },
};

export const shortDate = (iso: string) =>
  new Date(`${iso.slice(0, 10)}T12:00:00Z`).toLocaleDateString('es-MX', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  });

/** "vence el 28 nov 2026", or until the entrega of its pedido while that entrega is not programmed. */
export const expiry = (expiresOn?: string | null) => (expiresOn ? `vence el ${shortDate(expiresOn)}` : 'vale hasta la entrega de su pedido');

/** One beneficio: name, mechanic, code, state and until when. */
export function BeneficioLine({ b, children }: { b: Beneficio; children?: ReactNode }) {
  const state = STATE[b.state];
  // Stacked: name and state, then code and dates, then the actions.
  return (
    <li style={{ display: 'grid', gap: 2 }}>
      <span>
        <strong>{b.name}</strong> · {b.mechanic} <span className={`badge ${state.tone}`}>{state.text}</span>
      </span>
      <div className="muted">
        <code>{b.code}</code>
        {b.state === 'Usado' && b.usedAt ? ` · usado el ${shortDate(b.usedAt)}` : ` · ${expiry(b.expiresOn)}`}
        {b.state === 'Apartado' && b.pedidoId && ` · apartado para el pedido ${b.pedidoId}`}
        {b.condition && ` · ${b.condition}`}
      </div>
      {children}
    </li>
  );
}

/**
 * The invitado's sellos and cupones (Clientes › ficha): their state, giving one by hand and marking one as used.
 * Granting, the 30% cap and what can be used are decided by the API.
 */
export function BeneficiosInvitado({ invitadoId }: { invitadoId: number }) {
  const { can } = useSession();
  const canUse = can('promociones', 'edit') || can('clientes-crm', 'edit') || can('pedidos', 'edit');
  const canGrant = canUse && can('promociones', 'view');
  const notify = useNotify();
  const list = useQuery({
    queryKey: beneficiosKey(invitadoId),
    queryFn: () => promocionesApi.beneficiosOf(invitadoId),
  });
  const promos = useQuery({
    queryKey: ['promociones'],
    queryFn: () => promocionesApi.list(),
    enabled: canGrant,
  });
  const [promocionId, setPromocionId] = useState('');
  const refetch = [beneficiosKey(invitadoId), ['promociones'], keys.crm];
  const grant = useSave(() => promocionesApi.grantBeneficio(invitadoId, { promocionId: Number(promocionId) }), {
    refetch,
    onDone: (r) => {
      notify(r.warning ? `${r.beneficio.name} entregado. ${r.warning}` : `${r.beneficio.name} entregado (${r.beneficio.code}).`);
      setPromocionId('');
    },
  });
  const use = useSave((code: string) => promocionesApi.useBeneficio(code), {
    refetch,
    onDone: (b) => notify(`${b.name} quedó usado.`),
  });
  const grantable = (promos.data?.promociones ?? []).filter((p) => p.active && p.programa !== 'Campana');

  return (
    <section className="stack">
      <h3>Beneficios</h3>
      {list.data?.length === 0 && <span className="muted">Sin sellos ni cupones.</span>}
      <ul className="plain-list">
        {list.data?.map((b) => (
          <BeneficioLine key={b.id} b={b}>
            {canUse && b.state === 'Disponible' && (
              <button type="button" className="small" style={{ justifySelf: 'start' }} disabled={use.isPending} onClick={() => use.mutate(b.code)}>
                Marcar como usado
              </button>
            )}
          </BeneficioLine>
        ))}
      </ul>
      {list.error && <div className="alert error">{list.error.message}</div>}
      {use.error && <div className="alert error">{use.error.message}</div>}
      {canGrant && grantable.length > 0 && (
        <div className="actions tight">
          <select value={promocionId} onChange={(e) => setPromocionId(e.target.value)} aria-label="Promoción a entregar">
            <option value="">Dar un sello o cupón…</option>
            {grantable.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name} ({p.programa === 'Sellos' ? 'sello' : 'cupón'})
              </option>
            ))}
          </select>
          <button type="button" className="small" disabled={!promocionId || grant.isPending} onClick={() => grant.mutate(undefined)}>
            Dar
          </button>
        </div>
      )}
      {grant.error && <div className="alert error">{grant.error.message}</div>}
    </section>
  );
}

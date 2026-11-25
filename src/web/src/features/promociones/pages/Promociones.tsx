import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import { percent, type Programa, PROGRAMA, type Promocion, validityText } from '../types/promociones';
import { boardKey } from '../services/queryKeys';
import '../../../shared/api/client';
import { ActiveBadge, PageHeader, QueryState } from '../../../shared/components/Page';
import { UseByCode } from '../components/UseByCode';
import { PromocionForm } from '../components/PromocionForm';
import { promocionesApi } from '../services/promocionesApi';

/**
 * Promociones (docs/pantallas/oficina/promociones.md): redemption against its goal, sellos and cuponera with their
 * numbers, and marking a beneficio as used by its code. Granting, reserving and the 30% cap are decided by the API.
 */
export function Promociones() {
  const canEdit = useSession().can('promociones', 'edit');
  const [tab, setTab] = useState<Programa>('Sellos');
  const [editing, setEditing] = useState<Promocion | 'new' | null>(null);
  const board = useQuery({
    queryKey: boardKey,
    queryFn: () => promocionesApi.list(),
  });
  const rows = board.data?.promociones.filter((p) => p.programa === tab) ?? [];
  const sellos = tab === 'Sellos';
  const inactiveCupones = tab === 'Cuponera' && rows.some((p) => !p.active);

  return (
    <>
      <PageHeader
        title="Promociones"
        description="Sellos para invitados recurrentes y cuponera de bienvenida. Se entregan solos al cerrar cada recolección; el descuento se aplica en el POS."
      >
        {canEdit && tab !== 'Campana' && (
          <button type="button" className="primary" onClick={() => setEditing('new')}>
            Nueva promoción
          </button>
        )}
      </PageHeader>

      <QueryState loading={board.isPending} error={board.error}>
        {board.data && (
          <>
            <div className="ficha__stats">
              {board.data.indicators.map((i) => (
                <div key={i.programa}>
                  <span className="muted">{i.programa === 'Sellos' ? 'Sellos canjeados' : 'Cuponera canjeada'}</span>
                  <strong>
                    {percent(i.redemptionRate)}{' '}
                    {i.redemptionRate != null && (
                      <span className={`badge ${i.meetsGoal ? 'success' : 'warning'}`}>{i.meetsGoal ? 'en meta' : 'bajo la meta'}</span>
                    )}
                  </strong>
                  <span className="muted">
                    {i.used} de {i.granted} · meta {i.goal}% o más
                  </span>
                </div>
              ))}
              <div>
                <span className="muted">Campañas</span>
                <strong>—</strong>
                <span className="muted">Llegan con las notificaciones</span>
              </div>
            </div>

            <UseByCode />

            <nav className="tabs">
              {(['Sellos', 'Cuponera', 'Campana'] as Programa[]).map((p) => (
                <button key={p} type="button" className={tab === p ? 'selected' : ''} onClick={() => setTab(p)}>
                  {PROGRAMA[p]}
                </button>
              ))}
            </nav>

            {tab === 'Campana' ? (
              <div className="alert">
                Las campañas (segmento del CRM por ruta y estatus, canal push o WhatsApp de marketing y envío programado) llegan con las
                notificaciones.
              </div>
            ) : (
              <section className="card">
                <div className="card__header">
                  <p className="muted">
                    {sellos
                      ? `Se da al cerrar la recolección de un servicio con la línea indicada, a partir del segundo servicio, y no se repite mientras el invitado tenga uno sin usar. Ningún sello debe pasar del ${board.data.selloCap}% de los otorgados.`
                      : 'Al cerrar la recolección de su primer servicio, el invitado recibe un cupón de cada promoción activa. Un cupón inactivo no se entrega.'}
                  </p>
                  {inactiveCupones && (
                    <div className="alert">
                      Captura el beneficio impreso de cada cupón y actívalo: mientras estén inactivos, los invitados nuevos no reciben la cuponera
                      digital.
                    </div>
                  )}
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Promoción</th>
                        <th>Mecánica</th>
                        {sellos && <th>Se da al cerrar</th>}
                        <th>Vigencia</th>
                        <th className="num">Otorgados</th>
                        <th className="num">Usados</th>
                        <th className="num">Apartados</th>
                        <th className="num">Canje</th>
                        {sellos && <th className="num">De los sellos</th>}
                        <th>Estado</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.length === 0 && (
                        <tr>
                          <td colSpan={sellos ? 11 : 9} className="muted">
                            Sin promociones todavía.
                          </td>
                        </tr>
                      )}
                      {rows.map((p) => (
                        <tr key={p.id} className={p.active ? '' : 'inactive'}>
                          <td>
                            <strong>{p.name}</strong>
                            {p.condition && <div className="muted">{p.condition}</div>}
                          </td>
                          <td>{p.mechanic}</td>
                          {sellos && <td>{p.triggerLineaName ? `Servicio de ${p.triggerLineaName}` : 'Cualquier servicio'}</td>}
                          <td>{validityText(p.validityDays)}</td>
                          <td className="num">{p.granted}</td>
                          <td className="num">{p.used}</td>
                          <td className="num">{p.reserved}</td>
                          <td className="num">{percent(p.redemptionRate)}</td>
                          {sellos && (
                            <td className="num">
                              {percent(p.sharePercent)}
                              {p.overCap && (
                                <span className="badge warning" title={`Pasa del ${board.data.selloCap}% de los sellos otorgados`}>
                                  tope
                                </span>
                              )}
                            </td>
                          )}
                          <td>
                            <ActiveBadge active={p.active} />
                          </td>
                          <td className="end">
                            {canEdit && (
                              <button type="button" className="small" onClick={() => setEditing(p)}>
                                Editar
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}
          </>
        )}
      </QueryState>

      {editing && <PromocionForm promocion={editing === 'new' ? null : editing} programa={tab} onClose={() => setEditing(null)} />}
    </>
  );
}

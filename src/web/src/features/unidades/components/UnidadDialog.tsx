import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { type Schemas } from '../../../shared/api/client';
import { detailKey, listKey } from '../services/queryKeys';
import { useAction } from '../../../shared/hooks/useSave';
import { useConfirm } from '../../../shared/components/Toasts';
import { todayInMexicoCity } from '../../../shared/utils/dates';
import { Dialog } from '../../../shared/components/Dialog';
import { QueryState } from '../../../shared/components/Page';
import { UnidadCard } from './UnidadCard';
import { day, DOCUMENT, EVENT, FREQUENCY, km, money, SEVERITY } from '../types/unidades';
import { DocumentLink } from './DocumentLink';
import { UnidadForm } from './UnidadForm';
import { EventForm } from './EventForm';
import { DocumentForm } from './DocumentForm';
import { PaymentForm } from './PaymentForm';
import { unidadesApi } from '../services/unidadesApi';

type Action = 'edit' | 'event' | 'document' | 'photo' | { payment: Schemas['RecurringPaymentDto'] | null } | null;

export function UnidadDialog({ id, canEdit, onClose }: { id: number; canEdit: boolean; onClose: () => void }) {
  const detail = useQuery({ queryKey: detailKey(id), queryFn: () => unidadesApi.get(id) });
  const [action, setAction] = useState<Action>(null);
  const [tab, setTab] = useState<'bitacora' | 'documentos' | 'pagos'>('bitacora');
  const resolve = useAction((eventoId: number) => unidadesApi.resolveEvento(id, eventoId), [listKey, detailKey(id)], 'Falla marcada como resuelta.');
  const ask = useConfirm();
  const removeDocument = useAction(
    (documentoId: number) => unidadesApi.removeDocument(id, documentoId),
    [listKey, detailKey(id)],
    'Documento dado de baja.',
  );
  const pay = useAction(
    (p: Schemas['RecurringPaymentDto']) =>
      unidadesApi.addEvento(id, {
        kind: 'Pago',
        date: todayInMexicoCity(),
        km: null,
        amount: null,
        description: p.concept,
        severity: null,
        recurringPaymentId: p.id,
      }),
    [listKey, detailKey(id)],
    'Pago registrado; la siguiente fecha se movió.',
  );
  const d = detail.data;

  return (
    <>
      <Dialog title={d ? `Unidad ${d.card.plate}` : 'Unidad'} onClose={onClose} onSubmit={onClose} submitText="Cerrar">
        <QueryState loading={detail.isPending} error={detail.error}>
          {d && (
            <div className="stack unidad-detail">
              <div className="unidad-detail__top">
                <UnidadCard unidad={d.card} onOpen={() => undefined} />
                {canEdit && d.card.active && (
                  <div className="actions">
                    <button type="button" onClick={() => setAction('edit')}>
                      Editar datos
                    </button>
                    <button type="button" onClick={() => setAction('event')}>
                      Registrar evento
                    </button>
                    <button type="button" onClick={() => setAction('document')}>
                      Agregar documento
                    </button>
                    <button type="button" onClick={() => setAction('photo')}>
                      {d.card.photoId ? 'Cambiar foto' : 'Agregar foto'}
                    </button>
                    <button type="button" onClick={() => setAction({ payment: null })}>
                      Nuevo pago recurrente
                    </button>
                  </div>
                )}
              </div>
              {d.notes && <p className="muted">{d.notes}</p>}

              <div className="tabs" role="tablist">
                {(['bitacora', 'documentos', 'pagos'] as const).map((t) => (
                  <button
                    key={t}
                    type="button"
                    role="tab"
                    aria-selected={tab === t}
                    className={tab === t ? 'selected' : ''}
                    onClick={() => setTab(t)}
                  >
                    {t === 'bitacora'
                      ? `Bitácora (${d.events.length})`
                      : t === 'documentos'
                        ? `Documentos (${d.documents.length})`
                        : `Pagos recurrentes (${d.payments.length})`}
                  </button>
                ))}
              </div>

              {tab === 'bitacora' &&
                (d.events.length === 0 ? (
                  <div className="empty">Sin registros en la bitácora.</div>
                ) : (
                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr>
                          <th>Fecha</th>
                          <th>Tipo</th>
                          <th>Descripción</th>
                          <th>Km</th>
                          <th>Monto</th>
                          <th />
                        </tr>
                      </thead>
                      <tbody>
                        {d.events.map((e) => (
                          <tr key={e.id}>
                            <td>{day(e.date)}</td>
                            <td>
                              {EVENT[e.kind]}
                              {e.severity && <span className="badge danger"> {SEVERITY[e.severity]}</span>}
                            </td>
                            <td>{e.description}</td>
                            <td>{e.km != null ? km(e.km) : ''}</td>
                            <td>{e.amount != null ? money(e.amount) : ''}</td>
                            <td>
                              {e.kind === 'Falla' &&
                                (e.resolvedOn ? (
                                  <span className="muted">Resuelta {day(e.resolvedOn)}</span>
                                ) : canEdit ? (
                                  <button type="button" className="small" disabled={resolve.isPending} onClick={() => resolve.mutate(e.id)}>
                                    Marcar resuelta
                                  </button>
                                ) : (
                                  <span className="badge danger">Abierta</span>
                                ))}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}

              {tab === 'documentos' &&
                (d.documents.length === 0 ? (
                  <div className="empty">Sin documentos.</div>
                ) : (
                  <ul className="plain-list unidad-docs">
                    {d.documents.map((doc) => (
                      <li key={doc.id}>
                        <span>
                          <strong>{DOCUMENT[doc.kind]}</strong>
                          {doc.kind === 'Verificacion'
                            ? ` · hecha el ${day(doc.validFrom)}`
                            : doc.validUntil
                              ? ` · vigente hasta ${day(doc.validUntil)}`
                              : ''}
                          {doc.description && ` · ${doc.description}`}
                        </span>
                        <span className="actions tight">
                          {doc.hasFile && <DocumentLink unidadId={id} documentoId={doc.id} />}
                          {canEdit && (
                            <button
                              type="button"
                              className="small"
                              onClick={async () =>
                                (await ask('¿Dar de baja este documento?', { confirmText: 'Dar de baja', danger: true })) &&
                                removeDocument.mutate(doc.id)
                              }
                            >
                              Quitar
                            </button>
                          )}
                        </span>
                      </li>
                    ))}
                  </ul>
                ))}

              {tab === 'pagos' &&
                (d.payments.length === 0 ? (
                  <div className="empty">Sin pagos recurrentes (tenencia, crédito, póliza en pagos).</div>
                ) : (
                  <ul className="plain-list unidad-docs">
                    {d.payments.map((p) => (
                      <li key={p.id}>
                        <span>
                          <strong>{p.concept}</strong> · {money(p.amount)} · {FREQUENCY[p.frequency].toLowerCase()} · próximo {day(p.nextDate)}
                        </span>
                        {canEdit && (
                          <span className="actions tight">
                            <button
                              type="button"
                              className="small"
                              disabled={pay.isPending}
                              onClick={async () =>
                                (await ask(`¿Registrar hoy el pago de ${p.concept} por ${money(p.amount)}?`, { confirmText: 'Registrar pago' })) &&
                                pay.mutate(p)
                              }
                            >
                              Registrar pago
                            </button>
                            <button type="button" className="small" onClick={() => setAction({ payment: p })}>
                              Editar
                            </button>
                          </span>
                        )}
                      </li>
                    ))}
                  </ul>
                ))}
            </div>
          )}
        </QueryState>
      </Dialog>

      {/* Outside the detail: a form inside another form would submit (close) it too. */}
      {d && action === 'edit' && <UnidadForm detail={d} onClose={() => setAction(null)} onSaved={() => setAction(null)} />}
      {d && action === 'event' && <EventForm detail={d} onClose={() => setAction(null)} />}
      {d && action === 'document' && <DocumentForm detail={d} onClose={() => setAction(null)} />}
      {d && action === 'photo' && <DocumentForm detail={d} photo onClose={() => setAction(null)} />}
      {d && action && typeof action === 'object' && <PaymentForm unidadId={id} payment={action.payment} onClose={() => setAction(null)} />}
    </>
  );
}

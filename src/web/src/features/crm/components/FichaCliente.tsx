import { useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { resultName, type Row } from '../types/crm';
import { useSession } from '../../../shared/session/Session';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { QueryState } from '../../../shared/components/Page';
import { StatusBadge } from './StatusBadge';
import { formatDate, formatDateTime, shortDay } from '../../../shared/utils/dates';
import { EditDireccionButton, ZonaPicker, invitadosApi } from '../../invitados';
import { Seguimiento } from './Seguimiento';
import { BeneficiosInvitado } from '../../promociones';

export function Ficha({ id, onClose, onContact }: { id: number; onClose: () => void; onContact: (r: Row) => void }) {
  const canEdit = useSession().can('clientes-crm', 'edit');
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (d && !d.open) d.showModal();
  }, []);
  const ficha = useQuery({
    queryKey: [...keys.crm, 'ficha', id],
    queryFn: () => invitadosApi.ficha(id),
  });
  const f = ficha.data;

  return (
    <dialog ref={ref} onClose={onClose} className="ficha">
      <div className="dialog__header">
        <h2>{f?.invitado.name ?? 'Ficha'}</h2>
        {f && (
          <p className="muted">
            WhatsApp {f.invitado.whatsAppPhone}
            {f.invitado.howFoundUs && ` · ${f.invitado.howFoundUs}`}
          </p>
        )}
      </div>
      <div className="dialog__body stack">
        <QueryState loading={ficha.isPending} error={ficha.error}>
          {f && (
            <>
              <div className="ficha__stats">
                <div>
                  <span className="muted">Estatus</span>
                  <StatusBadge status={f.crm.status} manual={f.crm.statusIsManual} />
                </div>
                <div>
                  <span className="muted">Servicios</span>
                  <strong>{f.crm.serviceCount}</strong>
                </div>
                <div>
                  <span className="muted">Frecuencia</span>
                  <strong>{f.crm.frequencyDays ? `cada ${f.crm.frequencyDays} días` : '—'}</strong>
                </div>
                <div>
                  <span className="muted">Le toca</span>
                  <strong>{shortDay(f.crm.leTocaDate)}</strong>
                </div>
                <div>
                  <span className="muted">Último servicio</span>
                  <strong>{shortDay(f.crm.lastService)}</strong>
                </div>
              </div>
              <p className="muted">{f.crm.why}</p>

              <div className="ficha__grid">
                <section className="stack">
                  <h3>Direcciones</h3>
                  {f.invitado.addresses.length === 0 && <div className="alert">Sin direcciones.</div>}
                  {f.invitado.addresses.map((dir) => (
                    <div key={dir.id} className="address-option">
                      <span>
                        {dir.label && <strong>{dir.label}: </strong>}
                        {dir.street} {dir.exteriorNumber}
                        {dir.neighborhood && `, ${dir.neighborhood}`} · CP {dir.postalCode}
                      </span>
                      <div className="actions tight">
                        <ZonaPicker invitadoId={f.invitado.id} address={dir} />
                        <EditDireccionButton invitadoId={f.invitado.id} direccion={dir} />
                      </div>
                    </div>
                  ))}
                  {canEdit && <Seguimiento ficha={f} />}
                </section>

                <section className="stack">
                  <div className="actions" style={{ justifyContent: 'space-between' }}>
                    <h3>Contactos</h3>
                    {canEdit && (
                      <button type="button" className="small" onClick={() => onContact(f.crm)}>
                        Registrar contacto
                      </button>
                    )}
                  </div>
                  {f.contacts.length === 0 && <span className="muted">Sin contactos registrados.</span>}
                  <ul className="timeline">
                    {f.contacts.map((c) => (
                      <li key={c.id}>
                        <strong>
                          {c.channel} · {resultName(c.result)}
                        </strong>
                        <span className="muted">
                          {formatDateTime(c.contactedAt)}
                          {c.nextContactOn && ` · volver el ${shortDay(c.nextContactOn)}`}
                        </span>
                        {c.note && <span>{c.note}</span>}
                      </li>
                    ))}
                  </ul>

                  <h3>Pedidos</h3>
                  {f.pedidos.length === 0 && <span className="muted">Sin pedidos.</span>}
                  <ul className="timeline">
                    {f.pedidos.slice(0, 20).map((p) => (
                      <li key={p.id}>
                        <strong>{formatDate(p.recoleccionDate)}</strong>
                        <span className="muted">
                          {p.etapa} · Ruta {p.rutaCode ?? '—'} · {p.lineasServicio.join(', ') || 'Sin línea'}
                        </span>
                      </li>
                    ))}
                  </ul>
                  {f.pedidos.length > 20 && <span className="muted">y {f.pedidos.length - 20} más.</span>}
                  <BeneficiosInvitado invitadoId={f.invitado.id} />
                </section>
              </div>
            </>
          )}
        </QueryState>
      </div>
      <div className="dialog__footer">
        {f && (
          <Link className="button" to={`/agendar?invitado=${f.invitado.id}`}>
            Agendar recolección
          </Link>
        )}
        <button type="button" className="primary" onClick={onClose}>
          Cerrar
        </button>
      </div>
    </dialog>
  );
}

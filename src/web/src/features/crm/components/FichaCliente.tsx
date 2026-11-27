import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import { resultName, type Row } from '../types/crm';
import { useSession } from '../../../shared/session/Session';
import { keys } from '../../../shared/api/queryKeys';
import { type Schemas } from '../../../shared/api/client';
import { QueryState } from '../../../shared/components/Page';
import { StatusBadge } from './StatusBadge';
import { formatDate, formatDateTime, shortDay } from '../../../shared/utils/dates';
import { EditDireccionButton, ZonaPicker, invitadosApi } from '../../invitados';
import { Seguimiento } from './Seguimiento';
import { BeneficiosInvitado } from '../../promociones';
import { money } from '../../../shared/utils/format';
import { AppAccount } from './AppAccount';
import { DatosInvitado } from './DatosInvitado';
import { Duplicados } from './Duplicados';
import { Historia } from './Historia';

type FichaDto = Schemas['CrmFichaDto'];

const TABS = ['resumen', 'datos', 'direcciones', 'pedidos', 'contactos', 'beneficios'] as const;
type Tab = (typeof TABS)[number];

const PEDIDOS_SHOWN = 50;

/**
 * The invitado's ficha in the CRM, by tabs (docs/pantallas/oficina/clientes-crm.md, Ficha: mejora). The tabs of
 * empresas (Facturación, Contactos de la empresa) arrive with convenios.
 */
export function Ficha({ id, onClose, onContact }: { id: number; onClose: () => void; onContact: (r: Row) => void }) {
  const canEdit = useSession().can('clientes-crm', 'edit');
  const [tab, setTab] = useState<Tab>('resumen');
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

  const label = (t: Tab) =>
    t === 'resumen'
      ? 'Resumen'
      : t === 'datos'
        ? 'Datos'
        : t === 'direcciones'
          ? `Direcciones (${f?.invitado.addresses.length ?? 0})`
          : t === 'pedidos'
            ? `Pedidos (${f?.pedidos.length ?? 0})`
            : t === 'contactos'
              ? `Contactos (${f?.contacts.length ?? 0})`
              : 'Beneficios';

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
              <div className="tabs" role="tablist">
                {TABS.map((t) => (
                  <button key={t} type="button" role="tab" aria-selected={tab === t} className={tab === t ? 'selected' : ''} onClick={() => setTab(t)}>
                    {label(t)}
                  </button>
                ))}
              </div>
              {tab === 'resumen' && <Resumen f={f} canEdit={canEdit} />}
              {tab === 'datos' && <DatosInvitado ficha={f} />}
              {tab === 'direcciones' && <Direcciones f={f} />}
              {tab === 'pedidos' && <Pedidos f={f} />}
              {tab === 'contactos' && <Contactos f={f} canEdit={canEdit} onContact={onContact} />}
              {tab === 'beneficios' && <BeneficiosInvitado invitadoId={f.invitado.id} />}
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

/** Indicators, possible duplicates, app account, follow-up and the history. */
function Resumen({ f, canEdit }: { f: FichaDto; canEdit: boolean }) {
  return (
    <div className="stack">
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
        <div>
          <span className="muted">Gasto total</span>
          <strong>{money(f.value.totalSpent)}</strong>
        </div>
        <div>
          <span className="muted">Ticket promedio</span>
          <strong>
            {money(f.value.averageTicket)}
            {f.value.tickets > 0 && <span className="muted"> · {f.value.tickets} tickets</span>}
          </strong>
        </div>
      </div>
      <p className="muted">{f.crm.why}</p>
      <Duplicados invitadoId={f.invitado.id} name={f.invitado.name} />
      <div className="ficha__grid">
        <section className="stack">
          <AppAccount ficha={f} canEdit={canEdit} />
          {canEdit && <Seguimiento ficha={f} />}
        </section>
        <Historia invitadoId={f.invitado.id} />
      </div>
    </div>
  );
}

function Direcciones({ f }: { f: FichaDto }) {
  return (
    <section className="stack">
      {f.invitado.addresses.length === 0 && <div className="alert">Sin direcciones.</div>}
      {f.invitado.addresses.map((dir) => (
        <div key={dir.id} className="address-option">
          <span>
            {dir.label && <strong>{dir.label}: </strong>}
            {dir.street} {dir.exteriorNumber}
            {dir.neighborhood && `, ${dir.neighborhood}`} · CP {dir.postalCode}
            {dir.lat == null && <span className="badge warning crm-flag"> Sin ubicación en el mapa</span>}
          </span>
          <div className="actions tight">
            <ZonaPicker invitadoId={f.invitado.id} address={dir} />
            <EditDireccionButton invitadoId={f.invitado.id} direccion={dir} />
          </div>
        </div>
      ))}
    </section>
  );
}

function Pedidos({ f }: { f: FichaDto }) {
  const [all, setAll] = useState(false);
  const shown = all ? f.pedidos : f.pedidos.slice(0, PEDIDOS_SHOWN);
  return (
    <section className="stack">
      {f.pedidos.length === 0 && <span className="muted">Sin pedidos.</span>}
      <ul className="timeline">
        {shown.map((p) => (
          <li key={p.id}>
            <strong>
              <Link to={`/pedidos?pedido=${p.id}`}>Pedido {p.id}</Link> · {formatDate(p.recoleccionDate)}
            </strong>
            <span className="muted">
              {p.etapa} · Ruta {p.rutaCode ?? '—'} · {p.lineasServicio.join(', ') || 'Sin línea'}
            </span>
          </li>
        ))}
      </ul>
      {!all && f.pedidos.length > PEDIDOS_SHOWN && (
        <button type="button" className="link" onClick={() => setAll(true)}>
          Ver los {f.pedidos.length - PEDIDOS_SHOWN} anteriores
        </button>
      )}
    </section>
  );
}

function Contactos({ f, canEdit, onContact }: { f: FichaDto; canEdit: boolean; onContact: (r: Row) => void }) {
  return (
    <section className="stack">
      {canEdit && (
        <div className="actions">
          <button type="button" className="small" onClick={() => onContact(f.crm)}>
            Registrar contacto
          </button>
        </div>
      )}
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
    </section>
  );
}

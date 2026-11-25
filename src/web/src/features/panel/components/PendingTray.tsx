import { type ReactNode, useState } from 'react';
import { Link } from 'react-router';
import { KIND, type Panel } from '../types/panel';

type TrayList = 'pastOpen' | 'ready' | 'planta';

/** One counter per alert type; each opens exactly the list it counts. */
export function PendingTray({ panel }: { panel: Panel }) {
  const [open, setOpen] = useState<TrayList | null>(null);
  const { pastOpen, readyWithoutEntrega, longInPlanta } = panel.pending;
  const counters: { key: TrayList; count: number; label: string }[] = [
    { key: 'pastOpen', count: pastOpen.length, label: `parada${pastOpen.length === 1 ? '' : 's'} de días pasados sin cerrar` },
    {
      key: 'ready',
      count: readyWithoutEntrega.length,
      label: `pedido${readyWithoutEntrega.length === 1 ? '' : 's'} listo${readyWithoutEntrega.length === 1 ? '' : 's'} sin fecha de entrega`,
    },
    { key: 'planta', count: longInPlanta.length, label: `pedido${longInPlanta.length === 1 ? '' : 's'} con muchos días en planta` },
  ];
  return (
    <section className="panel-tray" aria-label="Pendientes">
      {counters.map((c) => (
        <button
          key={c.key}
          type="button"
          className={`panel-tray__item${c.count > 0 ? ' warn' : ''}`}
          disabled={c.count === 0}
          onClick={() => setOpen(open === c.key ? null : c.key)}
          aria-expanded={open === c.key}
        >
          <strong>{c.count}</strong> {c.label}
        </button>
      ))}
      <Link to="/unidades" className={`panel-tray__item${panel.fleetWithPending > 0 ? ' warn' : ''}`}>
        <strong>{panel.fleetWithPending}</strong> unidad{panel.fleetWithPending === 1 ? '' : 'es'} con pendientes
      </Link>

      {open === 'pastOpen' && (
        <TrayPanel title="Paradas de días pasados sin cerrar" action={<Link to="/rutas">Reprogramar en Rutas</Link>}>
          {pastOpen.map((x) => (
            <li key={x.paradaId}>
              <span>
                <strong>{x.name ?? 'Parada'}</strong> · {KIND[x.kind] ?? x.kind} · Ruta {x.rutaCode}
              </span>
              <Link to={`/rutas?fecha=${x.date}`}>{x.date}</Link>
            </li>
          ))}
        </TrayPanel>
      )}
      {open === 'ready' && (
        <TrayPanel title="Pedidos 100% listos sin fecha de entrega" action={<Link to="/pedidos">Confirmar entregas en Pedidos</Link>}>
          {readyWithoutEntrega.map((x) => (
            <li key={x.pedidoId}>
              <span>
                <strong>Pedido {x.pedidoId}</strong> · {x.invitadoName}
              </span>
              <span className="muted">
                listo hace {x.days} día{x.days === 1 ? '' : 's'}
              </span>
            </li>
          ))}
        </TrayPanel>
      )}
      {open === 'planta' && (
        <TrayPanel title="Pedidos con muchos días en planta" action={<Link to="/tickets-en-planta">Ver planta</Link>}>
          {longInPlanta.map((x) => (
            <li key={x.pedidoId}>
              <span>
                <strong>Pedido {x.pedidoId}</strong> · {x.invitadoName}
              </span>
              <span className="muted">{x.days} días en planta</span>
            </li>
          ))}
        </TrayPanel>
      )}
    </section>
  );
}

function TrayPanel({ title, action, children }: { title: string; action: ReactNode; children: ReactNode }) {
  return (
    <div className="panel-tray__list card">
      <div className="card__header">
        <h3>{title}</h3>
        {action}
      </div>
      <ul className="plain-list">{children}</ul>
    </div>
  );
}

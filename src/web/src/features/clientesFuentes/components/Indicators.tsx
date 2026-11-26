import { money, type Totals } from '../types/clientesFuentes';

type Item = { key: keyof Totals; name: string; hint: string; money?: boolean; onClick?: () => void };

/**
 * The indicators of the period, each with the value of what it is compared with (the same days of the previous
 * period while it is open). "Perdidos" opens the list.
 */
export function Indicators({ totals, previous, comparedLabel, onPerdidos }: { totals: Totals; previous: Totals; comparedLabel: string; onPerdidos: () => void }) {
  const items: Item[] = [
    { key: 'nuevos', name: 'Nuevos', hint: 'Su primer pedido fue en el periodo' },
    { key: 'recurrentes', name: 'Recurrentes', hint: 'Su pedido anterior fue hace 120 días o menos' },
    { key: 'recuperados', name: 'Recuperados', hint: 'Volvieron tras más de 120 días sin pedir' },
    { key: 'perdidos', name: 'Perdidos', hint: 'Cumplieron más de 120 días sin pedir. Clic para ver la lista', onClick: onPerdidos },
    { key: 'venta', name: 'Venta', hint: 'Tickets del punto de venta del periodo', money: true },
    { key: 'atendidos', name: 'Atendidos', hint: 'Invitados con un pedido en el periodo' },
    { key: 'activosAlCierre', name: 'Activos al cierre', hint: 'Pidieron en los 120 días previos al cierre' },
  ];
  return (
    <div className="cyf-indicators">
      {items.map((i) => {
        const value = totals[i.key] as number;
        const before = previous[i.key] as number;
        const content = (
          <>
            <span className="cyf-indicator__name">{i.name}</span>
            <strong>{i.money ? money(value) : value.toLocaleString('es-MX')}</strong>
            <span className="muted">
              {comparedLabel}: {i.money ? money(before) : before.toLocaleString('es-MX')}
            </span>
          </>
        );
        return i.onClick ? (
          <button key={i.key} type="button" className="cyf-indicator clickable" title={i.hint} onClick={i.onClick}>
            {content}
          </button>
        ) : (
          <div key={i.key} className="cyf-indicator" title={i.hint}>
            {content}
          </div>
        );
      })}
    </div>
  );
}

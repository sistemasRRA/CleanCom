import { money, type Panel } from '../types/panel';

export function Indicators({ panel }: { panel: Panel }) {
  const i = panel.indicators;
  return (
    <div className="planta-stats panel-stats">
      <Stat label="Recolecciones" value={`${i.recolecciones}/${i.recoleccionesPlanned}`} tone="brand" />
      <Stat label="Entregas" value={`${i.entregas}/${i.entregasPlanned}`} tone="brand" />
      <Stat label="Cobrado de referencia" value={money(i.collected)} tone="success" />
      <Stat
        label="Llegadas a tiempo"
        value={i.onTimePercent == null ? '—' : `${i.onTimePercent}%`}
        tone={i.onTimePercent != null && i.onTimePercent < 80 ? 'warning' : 'success'}
      />
      <Stat label="Pedidos en planta" value={String(i.inPlanta)} tone="brand" />
    </div>
  );
}

function Stat({ label, value, tone }: { label: string; value: string; tone: 'brand' | 'success' | 'warning' | 'danger' }) {
  return (
    <div className={`planta-stat ${tone}`}>
      <span className="planta-stat__label">{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

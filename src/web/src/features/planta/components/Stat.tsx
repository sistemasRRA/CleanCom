export function Stat({
  value,
  label,
  tone,
  note,
}: {
  value: number;
  label: string;
  tone: 'brand' | 'success' | 'warning' | 'danger';
  note?: string;
}) {
  return (
    <div className={`planta-stat ${tone}`}>
      <span className="planta-stat__label">{label}</span>
      <strong>{value}</strong>
      {note && <span className="planta-stat__note">{note}</span>}
    </div>
  );
}

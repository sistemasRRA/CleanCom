import { type Weekday, WEEKDAYS } from '../../../shared/utils/dates';

export function WeekdayBadges({ days }: { days: Weekday[] }) {
  if (days.length === 0) return <span className="muted">sin días</span>;
  return (
    <span className="actions tight">
      {WEEKDAYS.filter((d) => days.includes(d.key)).map((d) => (
        <span key={d.key} className="badge brand" title={d.name}>
          {d.name.slice(0, 3)}
        </span>
      ))}
    </span>
  );
}

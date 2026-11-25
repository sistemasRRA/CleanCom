import { type Status, STATUS } from '../types/crm';

export function StatusBadge({ status, manual }: { status: Status; manual?: boolean }) {
  const s = STATUS[status];
  return (
    <span className={`badge ${s.tone}`} title={manual ? 'Fijado por oficina' : undefined}>
      {s.name}
      {manual && ' ✎'}
    </span>
  );
}

import { useState } from 'react';
import type { Zona } from '../types/rutasZonas';
import { type Weekday, WEEKDAYS } from '../../../shared/utils/dates';
import { useSave } from '../../../shared/hooks/useSave';
import '../../../shared/api/client';
import { keys } from '../../../shared/api/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Checkbox } from '../../../shared/components/Form';
import { operacionApi } from '../services/operacionApi';

export function VisitDaysForm({ zona, onClose }: { zona: Zona; onClose: () => void }) {
  const [days, setDays] = useState<Weekday[]>(zona.visitDays);
  const save = useSave(() => operacionApi.updateZonaDays(zona.id, { days }), {
    refetch: [keys.zonas],
    message: 'Días de visita actualizados.',
    onDone: onClose,
  });
  const toggle = (d: Weekday) => setDays((list) => (list.includes(d) ? list.filter((x) => x !== d) : [...list, d]));

  return (
    <Dialog
      title={`Días de visita · ${zona.code}`}
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      error={save.error}
    >
      <p className="muted">Sus zonas EXT toman estos mismos días.</p>
      <div className="actions">
        {WEEKDAYS.map((d) => (
          <span key={d.key} style={{ minWidth: 110 }}>
            <Checkbox checked={days.includes(d.key)} onChange={() => toggle(d.key)}>
              {d.name}
            </Checkbox>
          </span>
        ))}
      </div>
    </Dialog>
  );
}

import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import type { Card, Panel } from '../types/panel';
import { useConfirm, useNotify } from '../../../shared/components/Toasts';
import { todayInMexicoCity } from '../../../shared/utils/dates';
import { panelKey } from '../services/queries';
import { keys } from '../../../shared/api/queryKeys';
import { ApiError } from '../../../shared/api/client';
import { rutasApi } from '../../rutas';

/** Chofer and unidad of the day, chosen from the card. A unidad with something pending asks to confirm. */
export function Assignments({ card: r, panel, date, canEdit }: { card: Card; panel: Panel; date: string; canEdit: boolean }) {
  const queryClient = useQueryClient();
  const notify = useNotify();
  const ask = useConfirm();
  const [busy, setBusy] = useState(false);
  const past = date < todayInMexicoCity();

  /** Runs a change and refreshes; an action that returns `false` was cancelled by the user (no message). */
  async function run(action: () => Promise<unknown>, message: string) {
    setBusy(true);
    try {
      if ((await action()) === false) return;
      await Promise.all([queryClient.invalidateQueries({ queryKey: panelKey }), queryClient.invalidateQueries({ queryKey: keys.rutasDia })]);
      notify(message);
    } catch (e) {
      notify((e as Error).message, true);
    } finally {
      setBusy(false);
    }
  }

  const setChofer = (operadorId: number | null) => run(() => rutasApi.assignChofer(r.rutaId, date, { operadorId }), 'Chofer asignado.');

  const setUnidad = (unidadId: number | null) =>
    run(async () => {
      const put = (confirm: boolean) => rutasApi.assignUnidad(r.rutaId, date, { unidadId, confirm });
      try {
        await put(false);
      } catch (e) {
        // 409 with a question: the unidad has something pending; the API says what.
        if (!(e instanceof ApiError && e.status === 409 && e.message.includes('¿'))) throw e;
        if (!(await ask(e.message, { confirmText: 'Asignar de todos modos' }))) return false;
        await put(true);
      }
    }, 'Unidad asignada.');

  const operadores = panel.operadores;
  const unidades = panel.unidades;
  const disabled = !canEdit || busy || past;
  return (
    <div className="form-grid panel-assign">
      <label className="field">
        <span>Chofer</span>
        <select value={r.operadorId ?? ''} disabled={disabled} onChange={(e) => void setChofer(e.target.value ? Number(e.target.value) : null)}>
          <option value="">{r.operadorIsUsual ? 'Chofer habitual' : 'Sin chofer'}</option>
          {operadores.map((o) => (
            <option key={o.id} value={o.id}>
              {o.name}
              {r.operadorIsUsual && r.operadorId === o.id ? ' (habitual)' : ''}
            </option>
          ))}
        </select>
      </label>
      <label className="field">
        <span>Unidad</span>
        <select value={r.unidadId ?? ''} disabled={disabled} onChange={(e) => void setUnidad(e.target.value ? Number(e.target.value) : null)}>
          <option value="">Sin unidad</option>
          {unidades.map((u) => (
            <option key={u.id} value={u.id} title={u.note ?? undefined}>
              {u.name}
              {u.note ? ' ⚠' : ''}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}

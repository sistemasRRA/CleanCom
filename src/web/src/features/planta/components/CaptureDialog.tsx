import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { keys } from '../../../shared/api/queryKeys';
import '../../../shared/api/client';
import { useSave } from '../../../shared/hooks/useSave';
import { refetch } from '../services/queryKeys';
import { Dialog } from '../../../shared/components/Dialog';
import { Field } from '../../../shared/components/Form';
import { plantaApi } from '../services/plantaApi';
import { operacionApi } from '../../operacion';

export function CaptureDialog({ onClose }: { onClose: () => void }) {
  const lineas = useQuery({ queryKey: keys.lineasServicio, queryFn: () => operacionApi.listLineasServicio() });
  const [pedidoId, setPedidoId] = useState('');
  const [folio, setFolio] = useState('');
  const [lineaId, setLineaId] = useState('');
  const [garments, setGarments] = useState('1');
  const [amount, setAmount] = useState('');
  const save = useSave(
    () =>
      plantaApi.captureTicket({
        folio: folio.trim(),
        pedidoId: Number(pedidoId),
        lineaServicioId: Number(lineaId),
        garments: Number(garments),
        amount: amount.trim() ? Number(amount) : null,
      }),
    {
      refetch,
      message: 'Ticket capturado.',
      onDone: () => {
        // Keeps the pedido and línea for the next ticket of the same pedido.
        setFolio('');
        setGarments('1');
        setAmount('');
      },
    },
  );

  return (
    <Dialog
      title="Capturar ticket"
      onClose={onClose}
      onSubmit={() => save.mutate(undefined)}
      busy={save.isPending}
      submitText="Capturar"
      error={save.error}
    >
      <p className="muted">Mientras se conecta el POS, los tickets se capturan aquí. El primer ticket pasa el pedido a En planta.</p>
      <div className="form-grid">
        <Field label="Pedido (folio)">
          <input inputMode="numeric" value={pedidoId} onChange={(e) => setPedidoId(e.target.value.replace(/\D/g, ''))} autoFocus />
        </Field>
        <Field label="Folio del ticket">
          <input value={folio} onChange={(e) => setFolio(e.target.value)} autoComplete="off" />
        </Field>
        <Field label="Línea de servicio">
          <select value={lineaId} onChange={(e) => setLineaId(e.target.value)}>
            <option value="">Elige la línea</option>
            {lineas.data
              ?.filter((l) => l.active)
              .map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                </option>
              ))}
          </select>
        </Field>
        <Field label="Prendas">
          <input type="number" min={1} value={garments} onChange={(e) => setGarments(e.target.value)} />
        </Field>
        <Field label="Monto (opcional)">
          <input type="number" min={0} step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
        </Field>
      </div>
    </Dialog>
  );
}

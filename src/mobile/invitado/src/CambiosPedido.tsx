import { useState } from 'react';
import { View } from 'react-native';
import { Alert, Body, Button, Card, TextField, Title, api, space, unwrap, type Schemas } from '@cleancom/mobile-shared';
import { Chip, day, stamp } from './ui';

type Seguimiento = Schemas['MySeguimientoDto'];
type Availability = Schemas['AvailabilityDto'];

/**
 * "Cambiar día" and "Cancelar" of a recolección still to be done. The API decides what can be done and until when
 * (canChangeDay, canCancel, cancelUntil); the confirmation is inline so it works the same on phones and on the web.
 */
export function CambiosPedido({ s, onDone }: { s: Seguimiento; onDone: (message: string) => void }) {
  const [mode, setMode] = useState<'none' | 'day' | 'cancel'>('none');
  const [options, setOptions] = useState<Availability | null>(null);
  const [date, setDate] = useState<string | null>(null);
  const [note, setNote] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const id = s.pedido.id;
  if (!s.canChangeDay) return null;

  async function openDays() {
    setMode('day');
    setError(null);
    setBusy(true);
    try {
      const o = await unwrap(api.GET('/yo/pedidos/{id}/cambiar-dia', { params: { path: { id } } }));
      setOptions(o);
      setDate(o.days.find((d) => d.available)?.date ?? null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function changeDay() {
    if (!date) return;
    setBusy(true);
    setError(null);
    try {
      const r = await unwrap(api.POST('/yo/pedidos/{id}/cambiar-dia', { params: { path: { id } }, body: { date } }));
      if (r.scheduled) {
        setMode('none');
        onDone(`Listo: pasamos el ${day(r.date, true)}.`);
      } else {
        // The day filled up meanwhile: show the message and the days again.
        setError(r.message);
        setOptions(r.availability);
        setDate(r.availability.days.find((d) => d.available)?.date ?? null);
      }
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function cancel() {
    setBusy(true);
    setError(null);
    try {
      await unwrap(api.POST('/yo/pedidos/{id}/cancelar', { params: { path: { id } }, body: { note: note.trim() || null } }));
      setMode('none');
      onDone('Cancelamos tu pedido.');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  const days = (options?.days ?? []).filter((d) => d.available).slice(0, 8);
  return (
    <Card>
      {error && <Alert>{error}</Alert>}
      {mode === 'none' && (
        <>
          <Title size="lg">¿Necesitas cambiar algo?</Title>
          <Button title="Cambiar día" variant="secondary" onPress={openDays} />
          {s.canCancel ? (
            <>
              <Button title="Cancelar pedido" variant="danger" onPress={() => (setMode('cancel'), setError(null))} />
              {s.cancelUntil && <Body muted>Puedes cancelar desde la app hasta el {stamp(s.cancelUntil)}</Body>}
            </>
          ) : (
            <Body muted>
              {s.cancelUntil ? `Desde la app se podía cancelar hasta el ${stamp(s.cancelUntil)}; para` : 'Para'} cancelarlo ahora, comunícate con oficina.
            </Body>
          )}
        </>
      )}
      {mode === 'day' && (
        <>
          <Title size="lg">Elige el nuevo día</Title>
          {options?.minimumDate && <Body muted>El día más próximo que podemos darte es el {day(options.minimumDate, true)}.</Body>}
          {!busy && days.length === 0 && <Body muted>No hay días disponibles en las próximas semanas; comunícate con oficina.</Body>}
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
            {days.map((d) => (
              <Chip key={d.date} label={day(d.date)} selected={date === d.date} onPress={() => setDate(d.date)} />
            ))}
          </View>
          <Button title={date ? `Cambiar al ${day(date)}` : 'Cambiar día'} onPress={changeDay} busy={busy} disabled={!date} />
          <Button title="Regresar" variant="ghost" onPress={() => (setMode('none'), setError(null))} />
        </>
      )}
      {mode === 'cancel' && (
        <>
          <Title size="lg">¿Cancelar la recolección del {day(s.pedido.recoleccionDate, true)}?</Title>
          <TextField label="¿Por qué? (opcional)" value={note} onChangeText={setNote} placeholder="Nos ayuda a mejorar" maxLength={300} />
          <Button title="Sí, cancelar" variant="danger" onPress={cancel} busy={busy} />
          <Button title="No, regresar" variant="ghost" onPress={() => (setMode('none'), setError(null))} />
        </>
      )}
    </Card>
  );
}

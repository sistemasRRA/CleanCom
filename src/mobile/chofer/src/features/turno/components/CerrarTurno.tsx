import { Alert, ApiError, Button, space, useTheme } from '@cleancom/mobile-shared';
import { useRef, useState } from 'react';
import { Alert as RNAlert, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { newLocalId, submit } from '../../../offline/cola';
import { Input, Section } from '../../../common/FormParts';
import { km, type Method, METHODS, money } from '../../../common/pagos';
import { parseAmount, parseKm, type Ruta, type Shift } from '../types/turno';
import { Sheet } from './Sheet';

/**
 * Fin de turno (docs/pantallas/apps/chofer.md, step 7): back at the base, the final km and what was collected per
 * forma de pago for the corte de caja. What the chofer recorded in the visits is shown as a reference; the chofer
 * declares what they actually have. The API asks to confirm a long shift or paradas still open.
 */
export function CerrarTurno({
  shift,
  onClose,
  onDone,
}: {
  shift: Shift;
  onClose: () => void;
  onDone: (ruta: Ruta | null, queued: boolean) => void;
}) {
  const { colors } = useTheme();
  const [kmText, setKmText] = useState('');
  const [amounts, setAmounts] = useState<Record<Method, string>>({ Efectivo: '', Tarjeta: '', Transferencia: '' });
  const [notes, setNotes] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const localId = useRef(newLocalId());

  const reading = parseKm(kmText);
  const startKm = shift.startKm ?? 0;
  const invalid = METHODS.filter((m) => amounts[m.key].trim() && parseAmount(amounts[m.key]) == null);

  async function send(confirm = false) {
    if (reading == null) {
      setError('Anota el kilometraje del tablero.');
      return;
    }
    if (invalid.length > 0) {
      setError(`Revisa el monto de ${invalid.map((m) => m.label.toLowerCase()).join(' y ')}.`);
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await submit({
        kind: 'turno-cerrar',
        paradaId: 0,
        body: {
          km: reading,
          // A forma left empty is declared as nothing collected.
          declared: METHODS.map((m) => ({ method: m.key, amount: parseAmount(amounts[m.key]) ?? 0 })),
          notes: notes.trim() || null,
          confirm,
          localId: localId.current,
          at: new Date().toISOString(),
        },
      });
      onDone(result.queued ? null : result.ruta, result.queued);
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        RNAlert.alert('¿Cerrar el turno?', e.message, [
          { text: 'Regresar', style: 'cancel' },
          { text: 'Cerrar turno', onPress: () => void send(true) },
        ]);
      } else {
        setError((e as Error).message);
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <Sheet title="Cerrar turno" subtitle="Kilometraje final y corte de caja" onClose={onClose}>
      {shift.openParadas > 0 && (
        <Alert tone="warning">
          {shift.openParadas === 1 ? 'Te queda 1 parada sin cerrar.' : `Te quedan ${shift.openParadas} paradas sin cerrar.`} Oficina las verá.
        </Alert>
      )}

      <Section title="Kilometraje final">
        <Input value={kmText} onChangeText={setKmText} placeholder={`Inicial: ${km(startKm)}`} keyboardType="number-pad" maxLength={10} />
        {reading != null && reading >= startKm && <Text style={{ color: colors.textMuted }}>Recorriste {km(reading - startKm)}.</Text>}
      </Section>

      <Section title="Lo que cobraste">
        {METHODS.map((m) => {
          const recorded = shift.recorded.find((r) => r.method === m.key);
          return (
            <View key={m.key} style={{ gap: space.xs }}>
              <Text style={{ color: colors.text, fontWeight: '700' }}>{m.label}</Text>
              <Input
                value={amounts[m.key]}
                onChangeText={(t) => setAmounts((a) => ({ ...a, [m.key]: t }))}
                placeholder="$0"
                keyboardType="decimal-pad"
              />
              <Text style={{ color: colors.textMuted, fontSize: 13 }}>
                {recorded
                  ? `En tus visitas anotaste ${money(recorded.amount)} (${recorded.count} ${recorded.count === 1 ? 'entrega' : 'entregas'}).`
                  : 'No anotaste cobros con esta forma de pago.'}
              </Text>
            </View>
          );
        })}
      </Section>

      <Section title="Notas (opcional)">
        <Input value={notes} onChangeText={setNotes} placeholder="Ej. faltan $20 de cambio" multiline maxLength={500} />
      </Section>

      <Text style={{ color: colors.textMuted }}>Oficina compara lo que declaras con lo anotado en las visitas y con el punto de venta.</Text>
      {error && <Alert>{error}</Alert>}
      <Button
        title="Cerrar turno"
        busy={busy}
        disabled={reading == null}
        icon={<Ionicons name="stop-circle" size={20} color={colors.onBrand} />}
        onPress={() => void send()}
      />
    </Sheet>
  );
}

import { Alert, ApiError, Button, useTheme } from '@cleancom/mobile-shared';
import { useRef, useState } from 'react';
import { Alert as RNAlert, Text } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { newLocalId, submit } from '../../../offline/cola';
import { Input, Section } from '../../../common/FormParts';
import { km } from '../../../common/pagos';
import { parseKm, type Ruta, type Unidad } from '../types/turno';
import { Sheet } from './Sheet';

/**
 * Inicio de turno (docs/pantallas/apps/chofer.md, step 1): the odometer reading of the unidad the office assigned.
 * The API checks the km against the unidad and asks to confirm a reading that looks like a typo.
 */
export function IniciarTurno({
  unidad,
  onClose,
  onDone,
}: {
  unidad: Unidad;
  onClose: () => void;
  onDone: (ruta: Ruta | null, queued: boolean) => void;
}) {
  const { colors } = useTheme();
  const [kmText, setKmText] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // The same id on every retry, so a resend after losing signal is not saved twice.
  const localId = useRef(newLocalId());
  const reading = parseKm(kmText);

  async function send(confirm = false) {
    if (reading == null) {
      setError('Anota el kilometraje del tablero.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await submit({
        kind: 'turno-iniciar',
        paradaId: 0,
        body: { km: reading, confirm, localId: localId.current, at: new Date().toISOString() },
      });
      onDone(result.queued ? null : result.ruta, result.queued);
    } catch (e) {
      if (e instanceof ApiError && e.status === 409) {
        RNAlert.alert('Revisa el kilometraje', e.message, [
          { text: 'Corregir', style: 'cancel' },
          { text: 'Sí, es correcto', onPress: () => void send(true) },
        ]);
      } else {
        setError((e as Error).message);
      }
    } finally {
      setBusy(false);
    }
  }

  return (
    <Sheet title="Iniciar turno" subtitle={`Unidad ${unidad.plate} · ${unidad.name}`} onClose={onClose}>
      {unidad.issues.length > 0 && <Alert tone="warning">{unidad.issues.join(' ')}</Alert>}
      <Section title="Kilometraje inicial">
        <Input
          value={kmText}
          onChangeText={setKmText}
          placeholder={unidad.km > 0 ? `Último registro: ${km(unidad.km)}` : 'Kilometraje del tablero'}
          keyboardType="number-pad"
          maxLength={10}
        />
        {unidad.km > 0 && <Text style={{ color: colors.textMuted }}>No puede ser menor que {km(unidad.km)}.</Text>}
      </Section>

      {error && <Alert>{error}</Alert>}
      <Button
        title="Iniciar turno"
        busy={busy}
        disabled={reading == null}
        icon={<Ionicons name="play" size={20} color={colors.onBrand} />}
        onPress={() => void send()}
      />
    </Sheet>
  );
}

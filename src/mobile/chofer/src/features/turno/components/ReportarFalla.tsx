import { Alert, Button, radius, space, useTheme } from '@cleancom/mobile-shared';
import { useRef, useState } from 'react';
import * as ImagePicker from 'expo-image-picker';
import { Image, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { newLocalId, submit } from '../../../offline/cola';
import { Choice, Input, Section } from '../../../common/FormParts';
import { type Ruta, type Severity, SEVERITIES, type Unidad } from '../types/turno';
import { Sheet } from './Sheet';

/**
 * Reportar falla de la unidad (docs/pantallas/apps/chofer.md, Otras funciones): gravedad, what happens and an
 * optional photo. It stays open in Unidades until the office marks it resolved.
 */
export function ReportarFalla({
  unidad,
  onClose,
  onDone,
}: {
  unidad: Unidad;
  onClose: () => void;
  onDone: (ruta: Ruta | null, queued: boolean) => void;
}) {
  const { colors } = useTheme();
  const [severity, setSeverity] = useState<Severity | null>(null);
  const [description, setDescription] = useState('');
  const [photo, setPhoto] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const localId = useRef(newLocalId());

  async function takePhoto() {
    setError(null);
    const permission = await ImagePicker.requestCameraPermissionsAsync();
    if (!permission.granted) {
      setError('Da permiso de cámara a CleanCom Chofer para tomar la foto.');
      return;
    }
    // Compressed on the phone before uploading.
    const shot = await ImagePicker.launchCameraAsync({ mediaTypes: ['images'], quality: 0.5, exif: false });
    if (!shot.canceled && shot.assets[0]) setPhoto(shot.assets[0].uri);
  }

  async function send() {
    if (!severity || !description.trim()) {
      setError('Elige la gravedad y describe la falla.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const result = await submit({
        kind: 'falla',
        paradaId: 0,
        body: { localId: localId.current, at: new Date().toISOString(), description: description.trim(), severity },
        fileUri: photo,
      });
      onDone(result.queued ? null : result.ruta, result.queued);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <Sheet title="Reportar falla" subtitle={`Unidad ${unidad.plate} · ${unidad.name}`} onClose={onClose}>
      <Section title="Gravedad">
        <View style={{ gap: space.sm }}>
          {SEVERITIES.map((s) => (
            <Choice
              key={s.key}
              selected={severity === s.key}
              label={`${s.label}: ${s.hint}`}
              tone={s.key === 'Alta' ? 'danger' : undefined}
              onPress={() => setSeverity(s.key)}
            />
          ))}
        </View>
      </Section>

      <Section title="¿Qué pasa?">
        <Input value={description} onChangeText={setDescription} placeholder="Ej. no enciende la luz de freno" multiline maxLength={900} />
      </Section>

      <Section title="Foto (opcional)">
        {photo && <Image source={{ uri: photo }} style={{ width: 120, height: 120, borderRadius: radius.sm }} />}
        <Button
          title={photo ? 'Tomar otra foto' : 'Tomar foto'}
          variant="secondary"
          icon={<Ionicons name="camera" size={20} color={colors.brand} />}
          onPress={() => void takePhoto()}
        />
      </Section>

      {severity === 'Alta' && (
        <Text style={{ color: colors.danger, fontWeight: '700' }}>Si no es seguro seguir, detente y llama a oficina.</Text>
      )}
      {error && <Alert>{error}</Alert>}
      <Button
        title="Reportar falla"
        busy={busy}
        disabled={!severity || !description.trim()}
        icon={<Ionicons name="construct" size={20} color={colors.onBrand} />}
        onPress={() => void send()}
      />
    </Sheet>
  );
}

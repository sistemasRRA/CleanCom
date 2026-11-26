import { Alert, Button, radius, type Schemas, space, useTheme } from '@cleancom/mobile-shared';
import { choferApi } from '../../../common/api/choferApi';
import { useEffect, useRef, useState } from 'react';
import * as ImagePicker from 'expo-image-picker';
import { Image, KeyboardAvoidingView, Modal, Platform, Pressable, ScrollView, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { type Parada, type Ruta } from '../types/visita';
import { type Method, METHODS } from '../../../common/pagos';
import { Firma, type Signature } from './Firma';
import { newLocalId, submit as submitAction } from '../../../offline/cola';
import { currentPosition } from '../../../common/ubicacion';
import { Choice, Input, Section, Stepper } from '../../../common/FormParts';

/**
 * Cerrar la visita (docs/pantallas/apps/chofer.md, step 5): done or not done, with what each kind needs. The API
 * checks the rules (photo, prendas, payment, motivo) and moves the pedido; the app only captures.
 */
export function CerrarVisita({
  parada: p,
  photoRequired,
  onClose,
  onDone,
}: {
  parada: Parada;
  photoRequired: boolean;
  onClose: () => void;
  /** The API's answer, or null with queued = true when it waits for signal. */
  onDone: (ruta: Ruta | null, queued: boolean) => void;
}) {
  const { colors } = useTheme();
  const [done, setDone] = useState(true);
  const [photos, setPhotos] = useState<string[]>([]);
  const [uploading, setUploading] = useState(false);
  const [garments, setGarments] = useState(p.garmentsApprox != null ? String(p.garmentsApprox) : '');
  const [notes, setNotes] = useState('');
  const [queuedPhotos, setQueuedPhotos] = useState(false);
  const [signature, setSignature] = useState<Signature | null>(null);
  const [method, setMethod] = useState<Method | null>(null);
  const [amount, setAmount] = useState('');
  const [reasons, setReasons] = useState<Schemas['ChoferReasonDto'][]>([]);
  const [reasonId, setReasonId] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // The same ids on every retry of this closing.
  const ids = useRef({ close: newLocalId(), notDone: newLocalId(), signature: newLocalId() });

  const isEntrega = p.kind === 'Entrega' || p.kind === 'RecoleccionYEntrega';
  const asksGarments = p.kind === 'Recoleccion' || p.kind === 'RecoleccionYEntrega';
  const name = p.invitadoName ?? 'la parada';
  const existingPhotos = p.photos + photos.length;

  useEffect(() => {
    choferApi
      .motivos()
      .then(setReasons)
      .catch(() => setReasons([]));
  }, []);

  async function takePhoto() {
    setError(null);
    const permission = await ImagePicker.requestCameraPermissionsAsync();
    if (!permission.granted) {
      setError('Da permiso de cámara a CleanCom Chofer para tomar la foto de evidencia.');
      return;
    }
    // Compressed on the phone before uploading.
    const shot = await ImagePicker.launchCameraAsync({ mediaTypes: ['images'], quality: 0.5, exif: false });
    if (shot.canceled || !shot.assets[0]) return;
    const asset = shot.assets[0];
    setUploading(true);
    try {
      const position = await currentPosition();
      const result = await submitAction({
        kind: 'foto',
        paradaId: p.id,
        body: { localId: newLocalId(), at: new Date().toISOString(), lat: position?.lat ?? null, lng: position?.lng ?? null },
        fileUri: asset.uri,
      });
      if (result.queued) setQueuedPhotos(true);
      setPhotos((list) => [...list, asset.uri]);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setUploading(false);
    }
  }

  async function submit() {
    setBusy(true);
    setError(null);
    try {
      const position = await currentPosition();
      const lat = position?.lat ?? null;
      const lng = position?.lng ?? null;
      const at = new Date().toISOString();
      let result;
      if (done) {
        if (signature) {
          await submitAction({ kind: 'firma', paradaId: p.id, body: { paradaId: p.id, localId: ids.current.signature, ...signature, takenAt: at } });
          // Not sent twice if the closing below fails and the chofer tries again.
          ids.current.signature = newLocalId();
          setSignature(null);
        }
        result = await submitAction({
          kind: 'cerrar',
          paradaId: p.id,
          body: {
            localId: ids.current.close,
            garments: asksGarments && garments.trim() ? Number(garments) : null,
            notes: notes.trim() || null,
            paymentMethod: method,
            paymentAmount: method && amount.trim() ? Number(amount.replace(',', '.')) : null,
            lat,
            lng,
            at,
          },
        });
      } else {
        result = await submitAction({
          kind: 'no-realizada',
          paradaId: p.id,
          body: { localId: ids.current.notDone, reasonId, note: notes.trim() || null, lat, lng, at },
        });
      }
      onDone(result.queued ? null : result.ruta, result.queued);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal visible animationType="slide" presentationStyle="pageSheet" onRequestClose={onClose}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{ flex: 1, backgroundColor: colors.bg }}>
        <View
          style={{
            flexDirection: 'row',
            alignItems: 'center',
            gap: space.sm,
            padding: space.lg,
            borderBottomWidth: 1,
            borderBottomColor: colors.border,
          }}
        >
          <View style={{ flex: 1 }}>
            <Text style={{ color: colors.text, fontSize: 20, fontWeight: '700' }}>Cerrar visita</Text>
            <Text style={{ color: colors.textMuted }}>
              {p.order}. {name}
            </Text>
          </View>
          <Pressable onPress={onClose} accessibilityRole="button" accessibilityLabel="Cancelar" hitSlop={8} style={{ padding: space.sm }}>
            <Ionicons name="close" size={26} color={colors.text} />
          </Pressable>
        </View>

        <ScrollView contentContainerStyle={{ padding: space.lg, gap: space.lg }} keyboardShouldPersistTaps="handled">
          <View style={{ flexDirection: 'row', gap: space.sm }}>
            <Choice selected={done} label="Se realizó" icon="checkmark-circle" onPress={() => setDone(true)} />
            <Choice selected={!done} label="No se realizó" icon="close-circle" tone="danger" onPress={() => setDone(false)} />
          </View>

          <Section title={`Foto de evidencia${photoRequired ? '' : ' (opcional)'}`}>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
              {photos.map((uri) => (
                <Image key={uri} source={{ uri }} style={{ width: 84, height: 84, borderRadius: radius.sm }} />
              ))}
            </View>
            {p.photos > 0 && <Text style={{ color: colors.textMuted }}>{p.photos} foto(s) ya guardadas.</Text>}
            {queuedPhotos && <Text style={{ color: colors.warning }}>Sin señal: las fotos se envían solas al volver la señal.</Text>}
            <Button
              title={existingPhotos > 0 ? 'Tomar otra foto' : done ? 'Tomar foto' : 'Tomar foto (por ejemplo, de la puerta)'}
              variant="secondary"
              busy={uploading}
              icon={<Ionicons name="camera" size={20} color={colors.brand} />}
              onPress={() => void takePhoto()}
            />
          </Section>

          {done && asksGarments && (
            <Section title={p.kind === 'Recoleccion' ? 'Prendas recogidas' : 'Prendas recogidas (opcional)'}>
              <Stepper value={garments} onChange={setGarments} />
            </Section>
          )}

          {done && isEntrega && (
            <>
              <Section title="Firma del invitado">
                <Firma onChange={setSignature} />
              </Section>
              <Section title="Pago de referencia (opcional)">
                <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
                  {METHODS.map((m) => (
                    <Choice
                      key={m.key}
                      small
                      selected={method === m.key}
                      label={m.label}
                      onPress={() => setMethod(method === m.key ? null : m.key)}
                    />
                  ))}
                </View>
                {method && <Input value={amount} onChangeText={setAmount} placeholder="Monto" keyboardType="decimal-pad" />}
              </Section>
            </>
          )}

          {!done && reasons.length > 0 && (
            <Section title="Motivo">
              <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
                {reasons.map((r) => (
                  <Choice key={r.id} small selected={reasonId === r.id} label={r.text} onPress={() => setReasonId(reasonId === r.id ? null : r.id)} />
                ))}
              </View>
            </Section>
          )}

          <Section title={done ? 'Notas (opcional)' : reasons.length > 0 ? 'Detalle' : 'Motivo'}>
            <Input value={notes} onChangeText={setNotes} placeholder={done ? 'Ej. dos sacos con mancha' : 'Ej. no había nadie'} multiline />
          </Section>

          {!done && (
            <Text style={{ color: colors.textMuted }}>
              {p.kind === 'Entrega' || p.kind === 'Recoleccion'
                ? 'Se reprograma solo al siguiente día con cupo y oficina lo verá.'
                : 'Oficina verá que no se realizó.'}
            </Text>
          )}
          {error && <Alert>{error}</Alert>}
          <Button
            title={done ? 'Cerrar visita' : 'Registrar no realizada'}
            busy={busy}
            disabled={uploading}
            icon={<Ionicons name={done ? 'checkmark' : 'alert-circle'} size={20} color={colors.onBrand} />}
            onPress={() => void submit()}
          />
        </ScrollView>
      </KeyboardAvoidingView>
    </Modal>
  );
}

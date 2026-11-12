import { useState } from 'react';
import { View } from 'react-native';
import { useSession } from '../session/Session';
import { Alert, Body, Button, Logo, TextField, Title } from '../ui/Controls';
import { Screen } from '../ui/Screen';
import { space } from '../theme/Theme';

/** First sign-in (or after a reset by the office): the initial password must be replaced. */
export function ChangePasswordScreen() {
  const { user, changePassword, logout } = useSession();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [repeat, setRepeat] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    if (next.length < 8) return setError('La contraseña nueva debe tener al menos 8 caracteres.');
    if (next !== repeat) return setError('Las contraseñas nuevas no coinciden.');
    setBusy(true);
    setError(null);
    try {
      await changePassword(current, next);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo cambiar la contraseña.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen center maxWidth={480}>
      <Logo height={36} />
      <View style={{ gap: space.xs }}>
        <Title>Cambia tu contraseña</Title>
        <Body muted>Hola, {user?.fullName}. Antes de continuar, cambia la contraseña que te dio oficina.</Body>
      </View>
      <TextField label="Contraseña actual" secret value={current} onChangeText={setCurrent} textContentType="password" />
      <TextField label="Contraseña nueva" hint="Mínimo 8 caracteres." secret value={next} onChangeText={setNext} textContentType="newPassword" />
      <TextField label="Repite la contraseña nueva" secret value={repeat} onChangeText={setRepeat} textContentType="newPassword" onSubmitEditing={submit} />
      {error && <Alert>{error}</Alert>}
      <Button title="Guardar y entrar" onPress={submit} busy={busy} disabled={!current || !next || !repeat} />
      <Button title="Salir" variant="ghost" onPress={() => void logout()} />
    </Screen>
  );
}

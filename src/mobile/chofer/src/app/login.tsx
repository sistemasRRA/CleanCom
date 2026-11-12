import { useState } from 'react';
import { View } from 'react-native';
import { Alert, Body, Button, Card, Logo, Screen, TextField, Title, apiUrl, space, useSession } from '@cleancom/mobile-shared';

/** The driver signs in with the short user name the office gave him (for example juan.rosales). */
export default function Login() {
  const { login } = useSession();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    if (!userName.trim() || !password) return;
    setBusy(true);
    setError(null);
    try {
      await login(userName.trim(), password);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo iniciar sesión.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen center maxWidth={480}>
      <View style={{ alignItems: 'center', gap: space.sm, marginBottom: space.md }}>
        <Logo height={44} />
        <Body muted center>
          App del chofer
        </Body>
      </View>
      <Card>
        <Title size="lg">Inicia tu turno</Title>
        <TextField
          label="Usuario"
          placeholder="juan.rosales"
          value={userName}
          onChangeText={setUserName}
          textContentType="username"
          returnKeyType="next"
        />
        <TextField label="Contraseña" secret value={password} onChangeText={setPassword} textContentType="password" onSubmitEditing={submit} />
        {error && <Alert>{error}</Alert>}
        <Button title="Entrar" onPress={submit} busy={busy} disabled={!userName.trim() || !password} />
      </Card>
      {__DEV__ && <Body muted center>Servidor: {apiUrl()}</Body>}
    </Screen>
  );
}

import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { router } from 'expo-router';
import { Alert, Body, Button, Card, Logo, Screen, TextField, Title, api, space, unwrap, useSession } from '@cleancom/mobile-shared';

/**
 * Sign in with the cell phone and a code by WhatsApp (POST /auth/codigo, POST /auth/verificar). A phone already in
 * the CRM finds its data and addresses; a new one is asked its name next.
 */
export default function Acceso() {
  const { signIn } = useSession();
  const [phone, setPhone] = useState('');
  const [sent, setSent] = useState<{ to: string; minutes: number } | null>(null);
  const [code, setCode] = useState('');
  const [wait, setWait] = useState(0);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const digits = phone.replace(/\D/g, '');
  const validPhone = digits.length === 10;

  // Countdown until a new code can be asked for.
  useEffect(() => {
    if (wait <= 0) return;
    const timer = setTimeout(() => setWait((w) => w - 1), 1000);
    return () => clearTimeout(timer);
  }, [wait]);

  async function sendCode() {
    setBusy(true);
    setError(null);
    try {
      const r = await unwrap(api.POST('/auth/codigo', { body: { phone: digits } }));
      setSent({ to: r.sentTo, minutes: r.expiresInMinutes });
      setWait(r.resendInSeconds);
      setCode('');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function verify(value = code) {
    setBusy(true);
    setError(null);
    try {
      const r = await unwrap(api.POST('/auth/verificar', { body: { phone: digits, code: value } }));
      await signIn(r.session);
      // A new invitado gives their name and "¿cómo nos conociste?" first.
      router.replace(r.newInvitado ? '/perfil' : '/');
    } catch (e) {
      setError((e as Error).message);
      setBusy(false);
    }
  }

  return (
    <Screen center maxWidth={480}>
      <View style={{ alignItems: 'center' }}>
        <Logo height={36} />
      </View>
      {!sent ? (
        <Card>
          <Title size="lg">Entra con tu celular</Title>
          <Body muted>Te mandamos un código por WhatsApp. Si ya eres cliente, encontramos tus datos y direcciones.</Body>
          <TextField
            label="Celular"
            placeholder="55 1234 5678"
            keyboardType="phone-pad"
            textContentType="telephoneNumber"
            autoComplete="tel"
            maxLength={16}
            value={phone}
            onChangeText={(t) => {
              setPhone(t);
              setError(null);
            }}
            onSubmitEditing={() => validPhone && void sendCode()}
            hint="10 dígitos, sin el +52."
            error={digits.length > 10 ? 'Son 10 dígitos.' : undefined}
          />
          {error && <Alert>{error}</Alert>}
          <Button title="Enviarme el código" disabled={!validPhone} busy={busy} onPress={() => void sendCode()} />
        </Card>
      ) : (
        <Card>
          <Title size="lg">Escribe el código</Title>
          <Body muted>
            Te lo mandamos por WhatsApp al {sent.to}. Vale {sent.minutes} minutos.
          </Body>
          <TextField
            autoFocus
            label="Código"
            placeholder="123456"
            keyboardType="number-pad"
            textContentType="oneTimeCode"
            autoComplete="sms-otp"
            maxLength={6}
            value={code}
            onChangeText={(t) => {
              const only = t.replace(/\D/g, '');
              setCode(only);
              setError(null);
              // Six digits: check right away, no need to press the button.
              if (only.length === 6 && !busy) void verify(only);
            }}
          />
          {error && <Alert>{error}</Alert>}
          <Button title="Entrar" disabled={code.length !== 6} busy={busy} onPress={() => void verify()} />
          <Button
            title={wait > 0 ? `Pedir otro código en ${wait} s` : 'Pedir otro código'}
            variant="ghost"
            disabled={wait > 0 || busy}
            onPress={() => void sendCode()}
          />
          <Button
            title="Cambiar el número"
            variant="ghost"
            onPress={() => {
              setSent(null);
              setError(null);
            }}
          />
        </Card>
      )}
      <View style={{ gap: space.sm }}>
        <Button title="Regresar" variant="ghost" onPress={() => router.back()} />
      </View>
    </Screen>
  );
}

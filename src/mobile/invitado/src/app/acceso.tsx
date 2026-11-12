import { useState } from 'react';
import { View } from 'react-native';
import { router } from 'expo-router';
import { Alert, Body, Button, Card, Logo, Screen, TextField, Title, space } from '@cleancom/mobile-shared';

/**
 * Sign in with phone + code by WhatsApp (SMS as backup). The API endpoints POST /auth/codigo and
 * POST /auth/verificar arrive in week 5 (docs/pantallas/apps/invitado.md); until then the screen
 * validates the number and explains it.
 */
export default function Acceso() {
  const [phone, setPhone] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const digits = phone.replace(/\D/g, '');
  const valid = digits.length === 10;

  return (
    <Screen center maxWidth={480}>
      <View style={{ alignItems: 'center' }}>
        <Logo height={36} />
      </View>
      <Card>
        <Title size="lg">Hola Mundo</Title>
        <Body muted>Te mandamos un código por WhatsApp. Si ya eres cliente, encontramos tus datos y direcciones.</Body>
        <TextField
          label="Celular"
          placeholder="55 1234 5678"
          keyboardType="phone-pad"
          textContentType="telephoneNumber"
          autoComplete="tel"
          maxLength={14}
          value={phone}
          onChangeText={(t) => {
            setPhone(t);
            setMessage(null);
          }}
          hint="10 dígitos, sin el +52."
          error={digits.length > 10 ? 'Son 10 dígitos.' : undefined}
        />
        {message && <Alert tone="info">{message}</Alert>}
        <Button
          title="Enviarme el código"
          disabled={!valid}
          onPress={() => setMessage('El acceso con celular se activa en la semana 5, cuando el API envíe el código por WhatsApp.')}
        />
      </Card>
      <Button title="Regresar" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

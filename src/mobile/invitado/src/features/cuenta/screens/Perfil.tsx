import { Alert, Body, Button, Card, Screen, space, TextField, Title } from '@cleancom/mobile-shared';
import { useEffect, useState } from 'react';
import { router } from 'expo-router';
import { View } from 'react-native';
import { useLoad } from '../../../common/hooks/useLoad';
import { Chip } from '../../../common/components/Chip';
import { cuentaApi } from '../services/cuentaApi';

/**
 * Mis datos (GET|PUT /yo). Right after signing up it asks the name and, once, "¿cómo nos conociste?" (options of the
 * office catalog). The phone is the sign-in and is not changed here.
 */
export function Perfil() {
  const me = useLoad(() => cuentaApi.me(), []);
  const options = useLoad(() => cuentaApi.motivos({ type: 'HowFoundUs' }), []);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [howFoundUs, setHowFoundUs] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const first = me.data ? !me.data.profileComplete : false;

  useEffect(() => {
    if (!me.data) return;
    // The name given at sign-up ("Invitado 5678") is not theirs: leave it empty to write it.
    setName(me.data.profileComplete || !me.data.name.startsWith('Invitado ') ? me.data.name : '');
    setEmail(me.data.email ?? '');
    setHowFoundUs(me.data.howFoundUs);
  }, [me.data]);

  async function save() {
    setBusy(true);
    setError(null);
    try {
      await cuentaApi.updateMe({
        name: name.trim(),
        email: email.trim() || null,
        otherPhone: me.data?.otherPhone ?? null,
        howFoundUs,
        receivesWhatsApp: me.data?.receivesWhatsApp ?? true,
      });
      if (router.canGoBack()) router.back();
      else router.replace('/');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen maxWidth={560}>
      <View style={{ gap: space.xs }}>
        <Title size="xxl">{first ? '¡Bienvenido a CleanCom!' : 'Mis datos'}</Title>
        <Body muted>
          {first
            ? 'Cuéntanos tu nombre para que el chofer sepa a quién buscar.'
            : `Tu celular es ${me.data?.whatsAppPhone ?? ''}; con él entras a la app.`}
        </Body>
      </View>
      {me.error && <Alert>{me.error}</Alert>}
      <Card>
        <TextField
          label="Nombre"
          placeholder="Nombre y apellido"
          autoComplete="name"
          textContentType="name"
          value={name}
          onChangeText={setName}
          maxLength={150}
        />
        <TextField
          label="Correo (opcional)"
          placeholder="tu@correo.com"
          keyboardType="email-address"
          autoCapitalize="none"
          autoComplete="email"
          value={email}
          onChangeText={setEmail}
          maxLength={150}
        />
      </Card>
      {(first || !me.data?.howFoundUs) && (
        <Card>
          <Body>¿Cómo nos conociste?</Body>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
            {(options.data ?? [])
              .filter((o) => o.active)
              .map((o) => (
                <Chip key={o.id} label={o.text} selected={howFoundUs === o.text} onPress={() => setHowFoundUs(o.text)} />
              ))}
          </View>
        </Card>
      )}
      {error && <Alert>{error}</Alert>}
      <Button title={first ? 'Continuar' : 'Guardar'} busy={busy} disabled={name.trim().length < 2 || !howFoundUs} onPress={() => void save()} />
      {!first && <Button title="Cancelar" variant="ghost" onPress={() => router.back()} />}
    </Screen>
  );
}

import { Alert, Body, Button, Screen, space, Title, useSession, useTheme } from '@cleancom/mobile-shared';
import { router } from 'expo-router';
import { Alert as NativeAlert, Pressable, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useLoad } from '../../../common/hooks/useLoad';
import { Row } from '../../../common/components/Row';
import { addressLine } from '../../../common/format';
import { cuentaApi } from '../services/cuentaApi';

/** Mi cuenta: my data (GET /yo), my addresses (/yo/direcciones), theme and sign out. */
export function Cuenta() {
  const { logout } = useSession();
  const { colors, scheme, toggleScheme } = useTheme();
  const me = useLoad(() => cuentaApi.me(), [], { reloadOnFocus: true });

  function remove(addressId: number, name: string) {
    NativeAlert.alert('¿Quitar esta dirección?', name, [
      { text: 'Cancelar', style: 'cancel' },
      {
        text: 'Quitar',
        style: 'destructive',
        onPress: () =>
          void cuentaApi
            .removeDireccion(addressId)
            .then(() => me.reload())
            .catch((e: Error) => NativeAlert.alert('No se pudo quitar', e.message)),
      },
    ]);
  }

  const d = me.data;
  return (
    <Screen>
      <Title size="xxl">Mi cuenta</Title>
      {me.error && <Alert>{me.error}</Alert>}
      {d && (
        <>
          <Row
            icon="person-outline"
            title={d.name}
            text={[d.whatsAppPhone, d.email].filter(Boolean).join(' · ')}
            onPress={() => router.push('/perfil')}
          />

          <View style={{ gap: space.sm }}>
            <Title size="lg">Mis direcciones</Title>
            {d.addresses.length === 0 && <Body muted>Agrega la dirección donde pasamos por tus prendas.</Body>}
            {d.addresses.map((a) => (
              <Row
                key={a.id}
                icon="location-outline"
                title={a.label || addressLine(a)}
                text={a.label ? addressLine(a) : (a.borough ?? undefined)}
                onPress={() => router.push({ pathname: '/direccion', params: { id: String(a.id) } })}
                right={
                  <View style={{ flexDirection: 'row', gap: space.lg }}>
                    <Pressable
                      accessibilityRole="button"
                      accessibilityLabel="Editar dirección"
                      hitSlop={8}
                      onPress={() => router.push({ pathname: '/direccion', params: { id: String(a.id) } })}
                    >
                      <Ionicons name="create-outline" size={20} color={colors.brand} />
                    </Pressable>
                    <Pressable
                      accessibilityRole="button"
                      accessibilityLabel="Quitar dirección"
                      hitSlop={8}
                      onPress={() => remove(a.id, a.label || addressLine(a))}
                    >
                      <Ionicons name="trash-outline" size={20} color={colors.danger} />
                    </Pressable>
                  </View>
                }
              />
            ))}
            <Button
              title="Agregar dirección"
              variant="secondary"
              onPress={() => router.push('/direccion')}
              icon={<Ionicons name="add" size={20} color={colors.text} />}
            />
          </View>
        </>
      )}
      <Button
        title={scheme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
        variant="secondary"
        onPress={toggleScheme}
        icon={<Ionicons name={scheme === 'dark' ? 'sunny-outline' : 'moon-outline'} size={20} color={colors.text} />}
      />
      <Button title="Cerrar sesión" variant="danger" onPress={() => void logout()} />
    </Screen>
  );
}

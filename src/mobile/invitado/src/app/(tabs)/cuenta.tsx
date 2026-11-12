import { View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Button, Card, Screen, Title, space, useSession, useTheme } from '@cleancom/mobile-shared';
import { usePreview } from '../../preview';

/** My data, addresses and notification preferences (GET|PUT /yo, /yo/direcciones). */
export default function Cuenta() {
  const { user, logout } = useSession();
  const { colors, scheme, toggleScheme } = useTheme();
  const { preview, setPreview } = usePreview();

  return (
    <Screen>
      <Title size="xxl">Mi cuenta</Title>
      {preview && <Alert tone="warning">Vista previa sin cuenta (solo desarrollo).</Alert>}
      <Card>
        <Body>{user?.fullName ?? 'Invitado'}</Body>
        <Body muted>Mis datos, mis direcciones y avisos (push y WhatsApp) llegan en la semana 6.</Body>
      </Card>
      <Button
        title={scheme === 'dark' ? 'Modo claro' : 'Modo oscuro'}
        variant="secondary"
        onPress={toggleScheme}
        icon={<Ionicons name={scheme === 'dark' ? 'sunny-outline' : 'moon-outline'} size={20} color={colors.text} />}
      />
      <View style={{ gap: space.sm }}>
        {preview ? (
          <Button title="Salir de la vista previa" variant="danger" onPress={() => setPreview(false)} />
        ) : (
          <Button title="Cerrar sesión" variant="danger" onPress={() => void logout()} />
        )}
      </View>
    </Screen>
  );
}

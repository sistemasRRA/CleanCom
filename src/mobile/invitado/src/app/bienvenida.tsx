import { View } from 'react-native';
import { router } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Body, Button, Logo, Screen, Title, space, useLayout, useTheme } from '@cleancom/mobile-shared';
import { usePreview } from '../preview';

const FEATURES: { icon: keyof typeof Ionicons.glyphMap; title: string; text: string }[] = [
  { icon: 'calendar-outline', title: 'Pide tu recolección', text: 'Elige el día disponible para tu zona en segundos.' },
  { icon: 'navigate-outline', title: 'Sigue a tu chofer', text: 'Mira en el mapa cuándo llega a tu puerta.' },
  { icon: 'gift-outline', title: 'Tus beneficios', text: 'Sellos y cupones siempre a la mano.' },
];

export default function Bienvenida() {
  const { colors } = useTheme();
  const { isTablet } = useLayout();
  const { setPreview } = usePreview();

  return (
    <Screen center maxWidth={isTablet ? 760 : 480}>
      <View style={{ alignItems: 'center', gap: space.md }}>
        <Logo height={isTablet ? 56 : 44} />
        <Title size="xxl">Tu tintorería a domicilio</Title>
        <Body muted center>
          Recogemos y entregamos tus prendas en CDMX.
        </Body>
      </View>

      <View style={{ flexDirection: isTablet ? 'row' : 'column', gap: space.md, marginVertical: space.lg }}>
        {FEATURES.map((f) => (
          <View
            key={f.title}
            style={{
              flex: isTablet ? 1 : undefined,
              flexDirection: isTablet ? 'column' : 'row',
              alignItems: isTablet ? 'center' : 'flex-start',
              gap: space.md,
              padding: space.lg,
              borderRadius: 16,
              backgroundColor: colors.surface,
              borderWidth: 1,
              borderColor: colors.border,
            }}
          >
            <Ionicons name={f.icon} size={28} color={colors.brand} />
            <View style={{ flex: isTablet ? undefined : 1, gap: 2, alignItems: isTablet ? 'center' : 'flex-start' }}>
              <Body>{f.title}</Body>
              <Body muted center={isTablet}>
                {f.text}
              </Body>
            </View>
          </View>
        ))}
      </View>

      <Button title="Entrar con mi celular" onPress={() => router.push('/acceso')} />
      {__DEV__ && <Button title="Ver la app sin cuenta (solo desarrollo)" variant="ghost" onPress={() => setPreview(true)} />}
    </Screen>
  );
}

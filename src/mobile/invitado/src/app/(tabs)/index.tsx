import { View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Card, Screen, Title, space, useLayout, useTheme } from '@cleancom/mobile-shared';

const STEPS: { icon: keyof typeof Ionicons.glyphMap; title: string; text: string }[] = [
  { icon: 'location-outline', title: 'Dirección', text: 'Una guardada o tu ubicación en el mapa.' },
  { icon: 'shirt-outline', title: 'Servicio', text: 'Línea de servicio, prendas aproximadas y notas para el chofer.' },
  { icon: 'calendar-outline', title: 'Fecha y turno', text: 'La primera fecha disponible de tu zona, u otro día; mañana, tarde o cualquier hora.' },
  { icon: 'pricetag-outline', title: 'Beneficios', text: 'Aplica tus sellos o cupones vigentes.' },
];

/** Ask for a pickup. Uses the same scheduling engine as the office (POST /agenda/disponibilidad), week 5. */
export default function Pedir() {
  const { colors } = useTheme();
  const { isTablet } = useLayout();

  return (
    <Screen maxWidth={isTablet ? 960 : 640}>
      <View style={{ gap: space.xs }}>
        <Title size="xxl">Pide tu servicio</Title>
        <Body muted>Recolección a domicilio en CDMX.</Body>
      </View>
      <Alert tone="info">Pedir servicio se conecta al motor de agenda en la semana 5.</Alert>
      {/* Two columns on tablets, one on phones. */}
      <View style={{ flexDirection: isTablet ? 'row' : 'column', flexWrap: 'wrap', gap: space.md }}>
        {STEPS.map((s, i) => (
          <Card key={s.title} style={{ flexBasis: isTablet ? '48%' : 'auto', flexGrow: 1, flexDirection: 'row', alignItems: 'flex-start' }}>
            <View
              style={{ width: 40, height: 40, borderRadius: 20, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}
            >
              <Ionicons name={s.icon} size={22} color={colors.brand} />
            </View>
            <View style={{ flex: 1, gap: 2 }}>
              <Body>
                {i + 1}. {s.title}
              </Body>
              <Body muted>{s.text}</Body>
            </View>
          </Card>
        ))}
      </View>
    </Screen>
  );
}

import { Body, Card, radius, type Schemas, space, Title, useTheme } from '@cleancom/mobile-shared';
import { View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { day } from '../../../common/format';

type Beneficio = Schemas['BeneficioDto'];

const STATE: Record<Schemas['BeneficioState'], string> = {
  Disponible: 'Disponible',
  Apartado: 'Apartado para tu pedido',
  Usado: 'Usado',
  Vencido: 'Vencido',
};

export function BeneficioCard({ b }: { b: Beneficio }) {
  const { colors } = useTheme();
  const live = b.state === 'Disponible' || b.state === 'Apartado';
  return (
    <Card style={{ opacity: live ? 1 : 0.6 }}>
      <View style={{ flexDirection: 'row', gap: space.md, alignItems: 'flex-start' }}>
        <View
          style={{
            width: 44,
            height: 44,
            borderRadius: 22,
            backgroundColor: live ? colors.successSoft : colors.surface2,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Ionicons name={b.programa === 'Sellos' ? 'ribbon' : 'gift'} size={22} color={live ? colors.success : colors.textMuted} />
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Title size="lg">{b.name}</Title>
          <Body>{b.mechanic}</Body>
          {b.condition && <Body muted>{b.condition}</Body>}
          <Body muted>
            {STATE[b.state]}
            {b.state === 'Usado' && b.usedAt
              ? ` el ${day(b.usedAt, true)}`
              : b.expiresOn
                ? ` · vence el ${day(b.expiresOn, true)}`
                : ' · vale hasta la entrega de tu pedido'}
          </Body>
        </View>
      </View>
      {live && (
        <View
          style={{
            borderWidth: 1,
            borderStyle: 'dashed',
            borderColor: colors.border,
            borderRadius: radius.md,
            padding: space.sm,
            alignItems: 'center',
          }}
        >
          <Body>
            Código: <Body>{b.code}</Body>
          </Body>
        </View>
      )}
    </Card>
  );
}

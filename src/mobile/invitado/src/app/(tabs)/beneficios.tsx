import { useCallback } from 'react';
import { View } from 'react-native';
import { useFocusEffect } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Card, Screen, Title, api, radius, space, unwrap, useTheme, type Schemas } from '@cleancom/mobile-shared';
import { day, useLoad } from '../../ui';

type Beneficio = Schemas['BeneficioDto'];

const STATE: Record<Schemas['BeneficioState'], string> = {
  Disponible: 'Disponible',
  Apartado: 'Apartado para tu pedido',
  Usado: 'Usado',
  Vencido: 'Vencido',
};

/**
 * Mis beneficios (GET /yo/beneficios): sellos and cupones with their code and expiry. The available ones go first; they
 * are applied when asking for a service, and the code is what the office or the POS asks for.
 */
export default function Beneficios() {
  const list = useLoad(() => unwrap(api.GET('/yo/beneficios')), []);
  useFocusEffect(
    useCallback(() => {
      void list.reload();
    }, [list.reload]),
  );
  const all = list.data ?? [];
  const current = all.filter((b) => b.state === 'Disponible' || b.state === 'Apartado');
  const past = all.filter((b) => b.state === 'Usado' || b.state === 'Vencido').slice(0, 20);

  return (
    <Screen>
      <Title size="xxl">Mis beneficios</Title>
      <Body muted>Tus sellos y cupones. Elígelos al pedir un servicio y los aplicamos en tu ticket.</Body>
      {list.error && <Alert>{list.error}</Alert>}
      {list.data && current.length === 0 && (
        <Alert tone="info">Aún no tienes beneficios disponibles. Al recoger tu primer servicio te regalamos tu cuponera de bienvenida.</Alert>
      )}
      {current.map((b) => (
        <BeneficioCard key={b.id} b={b} />
      ))}
      {past.length > 0 && (
        <>
          <Title size="lg">Usados y vencidos</Title>
          {past.map((b) => (
            <BeneficioCard key={b.id} b={b} />
          ))}
        </>
      )}
    </Screen>
  );
}

function BeneficioCard({ b }: { b: Beneficio }) {
  const { colors } = useTheme();
  const live = b.state === 'Disponible' || b.state === 'Apartado';
  return (
    <Card style={{ opacity: live ? 1 : 0.6 }}>
      <View style={{ flexDirection: 'row', gap: space.md, alignItems: 'flex-start' }}>
        <View style={{ width: 44, height: 44, borderRadius: 22, backgroundColor: live ? colors.successSoft : colors.surface2, alignItems: 'center', justifyContent: 'center' }}>
          <Ionicons name={b.programa === 'Sellos' ? 'ribbon' : 'gift'} size={22} color={live ? colors.success : colors.textMuted} />
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Title size="lg">{b.name}</Title>
          <Body>{b.mechanic}</Body>
          {b.condition && <Body muted>{b.condition}</Body>}
          <Body muted>
            {STATE[b.state]}
            {b.state === 'Usado' && b.usedAt ? ` el ${day(b.usedAt, true)}` : (b.expiresOn ? ` · vence el ${day(b.expiresOn, true)}` : ' · vale hasta la entrega de tu pedido')}
          </Body>
        </View>
      </View>
      {live && (
        <View style={{ borderWidth: 1, borderStyle: 'dashed', borderColor: colors.border, borderRadius: radius.md, padding: space.sm, alignItems: 'center' }}>
          <Body>
            Código: <Body>{b.code}</Body>
          </Body>
        </View>
      )}
    </Card>
  );
}

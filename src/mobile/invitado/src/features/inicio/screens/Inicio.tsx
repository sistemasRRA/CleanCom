import { Alert, Body, Button, Card, Screen, space, Title, useLayout, useTheme } from '@cleancom/mobile-shared';
import { router, useFocusEffect } from 'expo-router';
import { useCallback } from 'react';
import { Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useLoad } from '../../../common/hooks/useLoad';
import { Row } from '../../../common/components/Row';
import { day, ETAPA_ICON } from '../../../common/format';
import { pedidosApi } from '../../pedidos';
import { cuentaApi } from '../../cuenta';

/** Home: ask for a service and see the pedido in progress. */
export function Inicio() {
  const { colors } = useTheme();
  const { isTablet } = useLayout();
  const me = useLoad(() => cuentaApi.me(), []);
  const pedidos = useLoad(() => pedidosApi.mine(), []);
  // Back from asking for a service: show the new pedido.
  useFocusEffect(
    useCallback(() => {
      void pedidos.reload();
    }, [pedidos.reload]),
  );
  const current = (pedidos.data ?? []).filter((p) => p.etapa !== 'Entregado' && p.etapa !== 'Cancelado');
  const firstName = me.data?.name.split(' ')[0];

  return (
    <Screen maxWidth={isTablet ? 960 : 640}>
      <View style={{ gap: space.xs }}>
        <Title size="xxl">{firstName ? `Hola, ${firstName}` : 'Hola'}</Title>
        <Body muted>Recogemos y entregamos tus prendas a domicilio.</Body>
      </View>

      <Card style={{ backgroundColor: colors.navy, borderColor: colors.navy }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.md }}>
          <Ionicons name="shirt-outline" size={34} color="#fff" />
          <View style={{ flex: 1, gap: 2 }}>
            <Text style={{ color: '#fff', fontSize: 19, fontWeight: '700' }}>¿Pasamos por tus prendas?</Text>
            <Text style={{ color: 'rgba(255,255,255,0.8)', fontSize: 15 }}>Elige dirección, servicio y el día disponible de tu zona.</Text>
          </View>
        </View>
        <Button
          title="Pedir servicio"
          onPress={() => router.push('/pedir')}
          icon={<Ionicons name="add-circle-outline" size={20} color={colors.onBrand} />}
        />
      </Card>

      {pedidos.error && <Alert>{pedidos.error}</Alert>}
      {current.length > 0 && (
        <View style={{ gap: space.sm }}>
          <Title size="lg">En curso</Title>
          {current.map((p) => (
            <Row
              key={p.id}
              icon={ETAPA_ICON[p.etapa]}
              title={`${p.etapaName} · pedido ${p.id}`}
              text={
                p.etapa === 'Agendado'
                  ? `Recolección el ${day(p.recoleccionDate, true)}`
                  : p.entregaDate
                    ? `Entrega el ${day(p.entregaDate, true)}`
                    : p.address
              }
              onPress={() => router.push({ pathname: '/pedido/[id]', params: { id: String(p.id) } })}
            />
          ))}
        </View>
      )}
    </Screen>
  );
}

import { Alert, Body, Button, Screen, space, Title } from '@cleancom/mobile-shared';
import { router, useFocusEffect } from 'expo-router';
import { useCallback } from 'react';
import { View } from 'react-native';
import { useLoad } from '../../../common/hooks/useLoad';
import { Row } from '../../../common/components/Row';
import { day, ETAPA_ICON } from '../../../common/format';
import { pedidosApi } from '../services/pedidosApi';

/** Mis pedidos (GET /yo/pedidos): newest first; each opens its seguimiento. */
export function Pedidos() {
  const pedidos = useLoad(() => pedidosApi.mine(), []);
  useFocusEffect(
    useCallback(() => {
      void pedidos.reload();
    }, [pedidos.reload]),
  );
  const list = pedidos.data ?? [];

  return (
    <Screen>
      <Title size="xxl">Mis pedidos</Title>
      {pedidos.error && <Alert>{pedidos.error}</Alert>}
      {!pedidos.loading && list.length === 0 && (
        <View style={{ gap: space.md }}>
          <Body muted>Todavía no tienes pedidos.</Body>
          <Button title="Pedir servicio" onPress={() => router.push('/pedir')} />
        </View>
      )}
      <View style={{ gap: space.sm }}>
        {list.map((p) => (
          <Row
            key={p.id}
            icon={ETAPA_ICON[p.etapa]}
            title={`Pedido ${p.id} · ${p.etapaName}`}
            text={`Recolección ${day(p.recoleccionDate)}${p.entregaDate ? ` · entrega ${day(p.entregaDate)}` : ''}${p.lineasServicio.length ? ` · ${p.lineasServicio.join(', ')}` : ''}`}
            onPress={() => router.push({ pathname: '/pedido/[id]', params: { id: String(p.id) } })}
          />
        ))}
      </View>
    </Screen>
  );
}

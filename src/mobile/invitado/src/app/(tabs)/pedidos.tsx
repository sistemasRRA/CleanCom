import { Alert, Body, Screen, Title } from '@cleancom/mobile-shared';

/** Order history, stages and live tracking of the driver (GET /yo/pedidos, SignalR "seguimiento"). */
export default function Pedidos() {
  return (
    <Screen>
      <Title size="xxl">Mis pedidos</Title>
      <Body muted>Cada etapa de tu pedido y, cuando tu chofer va en camino, su ubicación en el mapa.</Body>
      <Alert tone="info">Disponible en la semana 5, junto con el seguimiento en tiempo real.</Alert>
    </Screen>
  );
}

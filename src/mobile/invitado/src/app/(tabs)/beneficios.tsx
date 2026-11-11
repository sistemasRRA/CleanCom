import { Alert, Body, Screen, Title } from '@cleancom/mobile-shared';

/** Stamps and coupons with expiry and code (GET /yo/beneficios). */
export default function Beneficios() {
  return (
    <Screen>
      <Title size="xxl">Mis beneficios</Title>
      <Body muted>Tus sellos y cupones, con su vencimiento y código.</Body>
      <Alert tone="info">Disponible en la semana 6, con Promociones.</Alert>
    </Screen>
  );
}

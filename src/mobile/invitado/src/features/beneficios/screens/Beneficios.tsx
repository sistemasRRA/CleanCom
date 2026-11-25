import { Alert, Body, Screen, Title } from '@cleancom/mobile-shared';
import { useLoad } from '../../../common/hooks/useLoad';
import { BeneficioCard } from '../components/BeneficioCard';
import { beneficiosApi } from '../services/beneficiosApi';

/**
 * Mis beneficios (GET /yo/beneficios): sellos and cupones with their code and expiry. The available ones go first; they
 * are applied when asking for a service, and the code is what the office or the POS asks for.
 */
export function Beneficios() {
  const list = useLoad(() => beneficiosApi.mine(), [], { reloadOnFocus: true });
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

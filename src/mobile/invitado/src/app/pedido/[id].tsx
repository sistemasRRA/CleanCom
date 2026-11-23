import { Text, View } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Button, Card, Screen, Title, api, space, unwrap, useTheme, type Schemas } from '@cleancom/mobile-shared';
import { ETAPA_ICON, TURNOS, day, stamp, useLoad } from '../../ui';

const VISIT: Partial<Record<Schemas['ParadaState'], string>> = {
  Pendiente: 'Tu visita es hoy.',
  EnCamino: 'Tu chofer va en camino.',
  EnSitio: 'Tu chofer llegó.',
};

/**
 * Seguimiento (GET /yo/pedidos/{id}/seguimiento): every etapa with its time and today's visit. The chofer on the
 * map, changing the day and cancelling arrive in week 6.
 */
export default function Pedido() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { colors } = useTheme();
  const s = useLoad(() => unwrap(api.GET('/yo/pedidos/{id}/seguimiento', { params: { path: { id: Number(id) } } })), [id]);
  const p = s.data?.pedido;
  const cancelled = p?.etapa === 'Cancelado';

  return (
    <Screen maxWidth={560}>
      <Button title="Regresar" variant="ghost" onPress={() => router.back()} icon={<Ionicons name="chevron-back" size={20} color={colors.brand} />} />
      {s.error && <Alert>{s.error}</Alert>}
      {p && (
        <>
          <View style={{ gap: space.xs }}>
            <Title size="xxl">Pedido {p.id}</Title>
            <Body muted>{p.address}</Body>
          </View>
          {s.data?.visitState && VISIT[s.data.visitState] && <Alert tone="info">{VISIT[s.data.visitState]}</Alert>}
          {cancelled && <Alert tone="warning">Este pedido se canceló.</Alert>}
          <Card>
            <Body>
              Recolección: {day(p.recoleccionDate, true)} · {TURNOS.find((t) => t.key === p.turno)?.label ?? 'Cualquier hora'}
            </Body>
            {p.entregaDate && <Body>Entrega: {day(p.entregaDate, true)}</Body>}
            {p.lineasServicio.length > 0 && <Body muted>{p.lineasServicio.join(', ')}</Body>}
            {p.garmentsApprox != null && <Body muted>{p.garmentsApprox} prendas aproximadas</Body>}
          </Card>
          {!cancelled && (
            <Card>
              {s.data!.steps.map((step, i, all) => (
                <View key={step.etapa} style={{ flexDirection: 'row', gap: space.md }}>
                  <View style={{ alignItems: 'center' }}>
                    <View
                      style={{
                        width: 36,
                        height: 36,
                        borderRadius: 18,
                        alignItems: 'center',
                        justifyContent: 'center',
                        backgroundColor: step.done ? (step.current ? colors.brand : colors.brandSoft) : colors.surface2,
                      }}
                    >
                      <Ionicons name={ETAPA_ICON[step.etapa]} size={18} color={step.current ? colors.onBrand : step.done ? colors.brand : colors.textMuted} />
                    </View>
                    {i < all.length - 1 && <View style={{ width: 2, flex: 1, minHeight: 14, backgroundColor: step.done && all[i + 1].done ? colors.brand : colors.border }} />}
                  </View>
                  <View style={{ flex: 1, paddingTop: 6, paddingBottom: space.md }}>
                    <Text style={{ color: step.done ? colors.text : colors.textMuted, fontSize: 16, fontWeight: step.current ? '700' : '500' }}>{step.name}</Text>
                    {step.at && step.done && <Body muted>{stamp(step.at)}</Body>}
                  </View>
                </View>
              ))}
            </Card>
          )}
        </>
      )}
    </Screen>
  );
}

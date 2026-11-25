import { Alert, Body, Button, Card, type Schemas, Screen, space, Title, useTheme } from '@cleancom/mobile-shared';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Text, View } from 'react-native';
import { useLoad } from '../../../common/hooks/useLoad';
import { MapaChofer } from '../components/MapaChofer';
import { day, ETAPA_ICON, stamp, TURNOS } from '../../../common/format';
import { CambiosPedido } from '../components/CambiosPedido';
import { pedidosApi } from '../services/pedidosApi';

const VISIT: Partial<Record<Schemas['ParadaState'], string>> = {
  Pendiente: 'Tu visita es hoy.',
  EnCamino: 'Tu chofer va en camino.',
  EnSitio: 'Tu chofer llegó.',
};

/** Seconds between reloads: often while the chofer is on the way, less on the day of the visit, never otherwise. */
const REFRESH: Partial<Record<Schemas['ParadaState'], number>> = { EnCamino: 10, Pendiente: 30, EnSitio: 30 };

/** "11:24 a.m." in Mexico City. */
const hour = (iso: string) => new Date(iso).toLocaleTimeString('es-MX', { hour: 'numeric', minute: '2-digit', timeZone: 'America/Mexico_City' });

/**
 * Seguimiento (GET /yo/pedidos/{id}/seguimiento): every etapa with its time, today's visit and, while the chofer is
 * on the way, the chofer on the map with the estimated arrival; "Cambiar día" and "Cancelar" while the recolección is
 * still to be done.
 */
export function Pedido() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { colors } = useTheme();
  const s = useLoad(() => pedidosApi.seguimiento(Number(id)), [id]);
  const p = s.data?.pedido;
  const cancelled = p?.etapa === 'Cancelado';
  const visit = s.data?.visitState ?? null;
  const chofer = s.data?.chofer ?? null;

  // Reload by itself on the day of the visit, so "en camino" and the chofer's position show up without touching anything.
  const [now, setNow] = useState(() => Date.now());
  const every = visit ? REFRESH[visit] : undefined;
  const { reload } = s;
  useEffect(() => {
    if (!every) return;
    const timer = setInterval(() => {
      setNow(Date.now());
      void reload();
    }, every * 1000);
    return () => clearInterval(timer);
  }, [every, reload]);
  const old = chofer?.signalLostAt != null && now >= Date.parse(chofer.signalLostAt);
  const [notice, setNotice] = useState<string | null>(null);

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
          {visit && VISIT[visit] && !chofer && <Alert tone="info">{VISIT[visit]}</Alert>}
          {chofer && (
            <Card>
              <View style={{ gap: space.xs }}>
                <Title size="lg">{chofer.name ? `${chofer.name} va en camino` : 'Tu chofer va en camino'}</Title>
                {chofer.estimatedArrival && !old && <Body>Llega aproximadamente a las {hour(chofer.estimatedArrival)}</Body>}
                {chofer.at == null && <Body muted>En cuanto su teléfono mande su ubicación, la verás aquí.</Body>}
                {old && chofer.at && <Body muted>Sin señal de su ubicación desde las {hour(chofer.at)}; aparece donde se vio por última vez.</Body>}
              </View>
              {(chofer.at != null || chofer.destinationLat != null) && <MapaChofer chofer={chofer} old={old} />}
            </Card>
          )}
          {notice && <Alert tone="success">{notice}</Alert>}
          {cancelled && !notice && <Alert tone="warning">Este pedido se canceló.</Alert>}
          <Card>
            <Body>
              Recolección: {day(p.recoleccionDate, true)} · {TURNOS.find((t) => t.key === p.turno)?.label ?? 'Cualquier hora'}
            </Body>
            {p.entregaDate && <Body>Entrega: {day(p.entregaDate, true)}</Body>}
            {p.lineasServicio.length > 0 && <Body muted>{p.lineasServicio.join(', ')}</Body>}
            {p.garmentsApprox != null && <Body muted>{p.garmentsApprox} prendas aproximadas</Body>}
          </Card>
          <CambiosPedido
            s={s.data!}
            onDone={(message) => {
              setNotice(message);
              void reload();
            }}
          />
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
                      <Ionicons
                        name={ETAPA_ICON[step.etapa]}
                        size={18}
                        color={step.current ? colors.onBrand : step.done ? colors.brand : colors.textMuted}
                      />
                    </View>
                    {i < all.length - 1 && (
                      <View
                        style={{ width: 2, flex: 1, minHeight: 14, backgroundColor: step.done && all[i + 1].done ? colors.brand : colors.border }}
                      />
                    )}
                  </View>
                  <View style={{ flex: 1, paddingTop: 6, paddingBottom: space.md }}>
                    <Text style={{ color: step.done ? colors.text : colors.textMuted, fontSize: 16, fontWeight: step.current ? '700' : '500' }}>
                      {step.name}
                    </Text>
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

import type { ReactNode } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Button, Card, Logo, Title, radius, space, touch, useLayout, useSession, useTheme } from '@cleancom/mobile-shared';

/**
 * Driver's day. Tablet in landscape: stops and map side by side (docs/pantallas/apps/chofer.md).
 * Phone or portrait: one above the other. The route itself arrives in week 2 (GET /chofer/ruta).
 */
export default function Turno() {
  const { user, logout } = useSession();
  const { colors, scheme, toggleScheme } = useTheme();
  const { gutter, isTablet, landscape } = useLayout();
  const sideBySide = isTablet && landscape;

  const stops = (
    <Panel title="Paradas del día" icon="list">
      <Alert tone="info">La ruta del día llega en la semana 2 del plan. Aquí verás tus paradas en orden con su hora estimada.</Alert>
      {['Recolección', 'Entrega', 'Convenio'].map((type, i) => (
        <PlaceholderStop key={type} order={i + 1} type={type} />
      ))}
    </Panel>
  );

  const map = (
    <Panel title="Mapa" icon="map" grow>
      <View
        style={{
          flex: 1,
          minHeight: 220,
          borderRadius: radius.md,
          backgroundColor: colors.surface2,
          alignItems: 'center',
          justifyContent: 'center',
          gap: space.sm,
        }}
      >
        <Ionicons name="navigate-circle-outline" size={48} color={colors.textMuted} />
        <Body muted center>
          El mapa con tus paradas y la navegación llegan en la semana 3.
        </Body>
      </View>
    </Panel>
  );

  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: colors.navy }} edges={['top', 'left', 'right']}>
      <StatusBar style="light" />
      {/* Navy bar, like the office web menu. */}
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.md, paddingHorizontal: gutter, paddingVertical: space.md }}>
        <View style={{ backgroundColor: '#fff', borderRadius: radius.md, paddingHorizontal: space.md, paddingVertical: space.sm }}>
          <Logo height={isTablet ? 26 : 20} variant={isTablet ? 'full' : 'mark'} background="light" />
        </View>
        <View style={{ flex: 1 }}>
          <Text style={{ color: '#fff', fontSize: 17, fontWeight: '700' }} numberOfLines={1}>
            {user?.fullName}
          </Text>
          <Text style={{ color: 'rgba(255,255,255,0.7)', fontSize: 13 }} numberOfLines={1}>
            {user?.roleName} · {new Date().toLocaleDateString('es-MX', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'America/Mexico_City' })}
          </Text>
        </View>
        <HeaderButton icon={scheme === 'dark' ? 'sunny' : 'moon'} label={scheme === 'dark' ? 'Modo claro' : 'Modo oscuro'} onPress={toggleScheme} />
        <HeaderButton icon="log-out-outline" label="Cerrar sesión" onPress={() => void logout()} />
      </View>

      <View style={{ flex: 1, backgroundColor: colors.bg }}>
        <ScrollView contentContainerStyle={{ padding: gutter, gap: space.lg, flexGrow: 1 }}>
          <Card style={{ flexDirection: isTablet ? 'row' : 'column', alignItems: isTablet ? 'center' : 'stretch' }}>
            <View style={{ flex: isTablet ? 1 : undefined, gap: space.xs }}>
              <Title size="lg">Inicio de turno</Title>
              <Body muted>Unidad, kilometraje inicial y foto del tablero. Disponible en la semana 7 del plan.</Body>
            </View>
            <Button title="Iniciar turno" onPress={() => undefined} disabled icon={<Ionicons name="play" size={18} color={colors.onBrand} />} />
          </Card>

          {sideBySide ? (
            <View style={{ flex: 1, flexDirection: 'row', gap: space.lg }}>
              <View style={{ width: '40%' }}>{stops}</View>
              <View style={{ flex: 1 }}>{map}</View>
            </View>
          ) : (
            <>
              {stops}
              {map}
            </>
          )}
        </ScrollView>
      </View>
    </SafeAreaView>
  );
}

function Panel({ title, icon, grow, children }: { title: string; icon: keyof typeof Ionicons.glyphMap; grow?: boolean; children: ReactNode }) {
  const { colors } = useTheme();
  return (
    <Card style={grow ? { flex: 1 } : undefined}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm }}>
        <Ionicons name={icon} size={20} color={colors.brand} />
        <Title size="lg">{title}</Title>
      </View>
      {children}
    </Card>
  );
}

function PlaceholderStop({ order, type }: { order: number; type: string }) {
  const { colors } = useTheme();
  return (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: space.md,
        minHeight: touch + 16,
        padding: space.md,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        opacity: 0.6,
      }}
    >
      <View style={{ width: 36, height: 36, borderRadius: 18, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}>
        <Text style={{ color: colors.brand, fontWeight: '700' }}>{order}</Text>
      </View>
      <View style={{ flex: 1, gap: 2 }}>
        <Text style={{ color: colors.text, fontWeight: '600', fontSize: 15 }}>{type}</Text>
        <Text style={{ color: colors.textMuted, fontSize: 13 }}>Ejemplo · dirección y hora estimada</Text>
      </View>
      <Ionicons name="chevron-forward" size={20} color={colors.textMuted} />
    </View>
  );
}

function HeaderButton({ icon, label, onPress }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void }) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={label}
      hitSlop={6}
      style={({ pressed }) => ({
        width: touch,
        height: touch,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: pressed ? 'rgba(255,255,255,0.18)' : 'rgba(255,255,255,0.08)',
        borderWidth: 1,
        borderColor: 'rgba(255,255,255,0.12)',
      })}
    >
      <Ionicons name={icon} size={22} color="#fff" />
    </Pressable>
  );
}

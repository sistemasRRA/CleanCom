import type { ReactNode } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Body, Button, Card, Logo, Title, radius, space, touch, useLayout, useSession, useTheme } from '@cleancom/mobile-shared';
import { RutaDelDia } from '../../ruta/RutaDelDia';

/**
 * Driver's day. Tablet in landscape: stops and map side by side (docs/pantallas/apps/chofer.md).
 * Phone or portrait: one above the other. Stops come from GET /chofer/ruta; the map arrives in week 3.
 */
export default function Turno() {
  const { user, logout } = useSession();
  const { colors, scheme, toggleScheme } = useTheme();
  const { gutter, isTablet, landscape } = useLayout();
  const sideBySide = isTablet && landscape;

  const stops = <RutaDelDia />;

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

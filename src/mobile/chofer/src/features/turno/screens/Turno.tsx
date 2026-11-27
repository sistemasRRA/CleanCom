import { Logo, radius, type Schemas, space, useLayout, useSession, useTheme } from '@cleancom/mobile-shared';
import { useState } from 'react';
import { SafeAreaView } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import { ScrollView, Text, View } from 'react-native';
import { stopTracking, useShiftTracking } from '../../../offline/locationTracking';
import { MapaRuta, RutaDelDia } from '../../ruta';
import { Panel } from '../components/Panel';
import { HeaderButton } from '../components/HeaderButton';
import { ShiftCard } from '../components/ShiftCard';
import { TrackingNotice } from '../components/TrackingNotice';

/**
 * Driver's day. Tablet in landscape: stops and map side by side (docs/pantallas/apps/chofer.md).
 * Phone or portrait: one above the other. Stops come from GET /chofer/ruta; the map shows the same day the list shows.
 */
export function Turno() {
  const { user, logout } = useSession();
  const { colors, scheme, toggleScheme } = useTheme();
  const { gutter, isTablet, landscape } = useLayout();
  const sideBySide = isTablet && landscape;
  const [ruta, setRuta] = useState<Schemas['ChoferRutaDto'] | null>(null);
  const tracking = useShiftTracking(ruta);

  const stops = <RutaDelDia onShown={setRuta} />;

  const map = (
    <Panel title="Mapa" icon="map" grow>
      <MapaRuta ruta={ruta} />
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
            {user?.roleName} ·{' '}
            {new Date().toLocaleDateString('es-MX', { weekday: 'long', day: 'numeric', month: 'long', timeZone: 'America/Mexico_City' })}
          </Text>
        </View>
        <HeaderButton icon={scheme === 'dark' ? 'sunny' : 'moon'} label={scheme === 'dark' ? 'Modo claro' : 'Modo oscuro'} onPress={toggleScheme} />
        <HeaderButton icon="log-out-outline" label="Cerrar sesión" onPress={() => void stopTracking().finally(() => void logout())} />
      </View>

      <View style={{ flex: 1, backgroundColor: colors.bg }}>
        <ScrollView contentContainerStyle={{ padding: gutter, gap: space.lg, flexGrow: 1 }}>
          <ShiftCard ruta={ruta} />
          <TrackingNotice state={tracking} />
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

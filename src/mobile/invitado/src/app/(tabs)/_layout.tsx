import Ionicons from '@expo/vector-icons/Ionicons';
import { useLayout, useTheme } from '@cleancom/mobile-shared';
import { Redirect, Tabs } from 'expo-router';
import type { ColorValue } from 'react-native';
import { useLoad } from '../../common/hooks/useLoad';
import { cuentaApi } from '../../features/cuenta';

type Icon = keyof typeof Ionicons.glyphMap;

/** Bottom tabs on phones; on tablets the same tabs become a side bar. */
export default function TabsLayout() {
  const { colors } = useTheme();
  const { isTablet } = useLayout();
  // Until the invitado gives their name and "¿cómo nos conociste?", that comes first.
  const me = useLoad(() => cuentaApi.me(), []);
  if (me.data && !me.data.profileComplete) return <Redirect href="/perfil" />;
  const icon =
    (name: Icon) =>
    ({ color, size }: { color: ColorValue; size: number }) => (
      <Ionicons name={name} size={size} color={typeof color === 'string' ? color : undefined} />
    );

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarPosition: isTablet ? 'left' : 'bottom',
        tabBarVariant: isTablet ? 'material' : 'uikit',
        tabBarActiveTintColor: colors.brand,
        tabBarInactiveTintColor: colors.textMuted,
        tabBarStyle: { backgroundColor: colors.surface, borderColor: colors.border },
        sceneStyle: { backgroundColor: colors.bg },
      }}
    >
      <Tabs.Screen name="index" options={{ title: 'Pedir', tabBarIcon: icon('add-circle-outline') }} />
      <Tabs.Screen name="pedidos" options={{ title: 'Mis pedidos', tabBarIcon: icon('receipt-outline') }} />
      <Tabs.Screen name="beneficios" options={{ title: 'Beneficios', tabBarIcon: icon('gift-outline') }} />
      <Tabs.Screen name="cuenta" options={{ title: 'Mi cuenta', tabBarIcon: icon('person-circle-outline') }} />
    </Tabs>
  );
}

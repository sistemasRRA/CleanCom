import { useEffect } from 'react';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { SessionProvider, ThemeProvider, useSession, useTheme } from '@cleancom/mobile-shared';

// Signed out, the app opens on the welcome screen (otherwise the router picks the first route by name, "acceso").
export const unstable_settings = { initialRouteName: 'bienvenida' };

void SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <ThemeProvider>
        <SessionProvider appScreen="app-invitado" appName="la app de CleanCom">
          <Routes />
        </SessionProvider>
      </ThemeProvider>
    </SafeAreaProvider>
  );
}

/** Signed in: the tabs and the screens opened from them. Signed out: welcome and sign-in. */
function Routes() {
  const { status } = useSession();
  const { colors } = useTheme();

  useEffect(() => {
    if (status !== 'loading') void SplashScreen.hideAsync();
  }, [status]);

  if (status === 'loading') return null;

  return (
    <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: colors.bg } }}>
      <Stack.Protected guard={status === 'signedIn'}>
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="perfil" />
        <Stack.Screen name="pedir" />
        <Stack.Screen name="direccion" />
        <Stack.Screen name="pedido/[id]" />
      </Stack.Protected>
      <Stack.Protected guard={status === 'mustChangePassword'}>
        <Stack.Screen name="cambiar-contrasena" />
      </Stack.Protected>
      <Stack.Protected guard={status === 'signedOut'}>
        <Stack.Screen name="bienvenida" />
        <Stack.Screen name="acceso" />
      </Stack.Protected>
    </Stack>
  );
}

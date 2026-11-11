import { useEffect } from 'react';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { SessionProvider, ThemeProvider, useSession, useTheme } from '@cleancom/mobile-shared';

// The splash stays until the saved session has been read, so the sign-in screen does not flash.
void SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <ThemeProvider>
        <SessionProvider appScreen="app-chofer" appName="la app del chofer">
          <Routes />
        </SessionProvider>
      </ThemeProvider>
    </SafeAreaProvider>
  );
}

/** Each screen is reachable only in its session state; the rest redirect. */
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
        <Stack.Screen name="(turno)" />
      </Stack.Protected>
      <Stack.Protected guard={status === 'mustChangePassword'}>
        <Stack.Screen name="cambiar-contrasena" />
      </Stack.Protected>
      <Stack.Protected guard={status === 'signedOut'}>
        <Stack.Screen name="login" />
      </Stack.Protected>
    </Stack>
  );
}

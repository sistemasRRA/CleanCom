import { useEffect } from 'react';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { SessionProvider, ThemeProvider, useSession, useTheme } from '@cleancom/mobile-shared';
import { PreviewProvider, usePreview } from '../preview';

void SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <ThemeProvider>
        <SessionProvider appScreen="app-invitado" appName="la app de CleanCom">
          <PreviewProvider>
            <Routes />
          </PreviewProvider>
        </SessionProvider>
      </ThemeProvider>
    </SafeAreaProvider>
  );
}

function Routes() {
  const { status } = useSession();
  const { preview } = usePreview();
  const { colors } = useTheme();
  const inside = status === 'signedIn' || preview;

  useEffect(() => {
    if (status !== 'loading') void SplashScreen.hideAsync();
  }, [status]);

  if (status === 'loading') return null;

  return (
    <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: colors.bg } }}>
      <Stack.Protected guard={inside}>
        <Stack.Screen name="(tabs)" />
      </Stack.Protected>
      <Stack.Protected guard={status === 'mustChangePassword' && !preview}>
        <Stack.Screen name="cambiar-contrasena" />
      </Stack.Protected>
      <Stack.Protected guard={status === 'signedOut' && !preview}>
        <Stack.Screen name="bienvenida" />
        <Stack.Screen name="acceso" />
      </Stack.Protected>
    </Stack>
  );
}

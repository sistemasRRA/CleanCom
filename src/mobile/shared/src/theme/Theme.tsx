import { createContext, use, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useColorScheme, useWindowDimensions } from 'react-native';
import * as SecureStore from 'expo-secure-store';

/** Same brand tokens as the office web (src/web/src/styles.css), taken from cleancom.mx. */
const light = {
  navy: '#14276d',
  brand: '#3e5c8a',
  brandStrong: '#2f4a73',
  brandSoft: '#eef4ff',
  brandLight: '#7fa3d6',
  onBrand: '#ffffff',
  bg: '#f5f8fc',
  surface: '#ffffff',
  surface2: '#eef2f7',
  border: '#dde3ec',
  text: '#0f172a',
  textMuted: '#667085',
  success: '#1f7a4d',
  successSoft: '#e2f3ea',
  warning: '#9a6200',
  warningSoft: '#fdf1dc',
  danger: '#b3261e',
  dangerSoft: '#fbe6e4',
};

const dark: typeof light = {
  navy: '#14276d',
  brand: '#7fa3d6',
  brandStrong: '#a5c0e6',
  brandSoft: '#1c2a44',
  brandLight: '#7fa3d6',
  onBrand: '#0f172a',
  bg: '#0d1424',
  surface: '#141d30',
  surface2: '#1b263d',
  border: '#2a3752',
  text: '#e6ebf3',
  textMuted: '#98a3b8',
  success: '#5cc98f',
  successSoft: '#173527',
  warning: '#f0b45a',
  warningSoft: '#3a2c12',
  danger: '#f08a82',
  dangerSoft: '#3d1d1b',
};

export type Colors = typeof light;
export type Scheme = 'light' | 'dark';

/** Spacing, radius and type scale. Touch targets are at least 48 points (gloves, moving truck). */
export const space = { xs: 4, sm: 8, md: 12, lg: 16, xl: 24, xxl: 32 } as const;
export const radius = { sm: 8, md: 12, lg: 16, pill: 999 } as const;
export const font = {
  regular: undefined,
  size: { sm: 13, md: 15, lg: 17, xl: 22, xxl: 28 },
} as const;
export const touch = 48;

const THEME_KEY = 'cleancom.theme';

type ThemeContext = { scheme: Scheme; colors: Colors; toggleScheme: () => void };
const Context = createContext<ThemeContext | null>(null);

/** Follows the phone's light or dark mode until the user picks one; the choice is remembered. */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const system = useColorScheme() === 'dark' ? 'dark' : 'light';
  const [chosen, setChosen] = useState<Scheme | null>(null);

  useEffect(() => {
    SecureStore.getItemAsync(THEME_KEY)
      .then((v) => {
        if (v === 'light' || v === 'dark') setChosen(v);
      })
      .catch(() => undefined);
  }, []);

  const value = useMemo<ThemeContext>(() => {
    const scheme = chosen ?? system;
    return {
      scheme,
      colors: scheme === 'dark' ? dark : light,
      toggleScheme: () => {
        const next: Scheme = scheme === 'dark' ? 'light' : 'dark';
        setChosen(next);
        SecureStore.setItemAsync(THEME_KEY, next).catch(() => undefined);
      },
    };
  }, [chosen, system]);

  return <Context value={value}>{children}</Context>;
}

export function useTheme() {
  const context = use(Context);
  if (!context) throw new Error('useTheme needs ThemeProvider.');
  return context;
}

/**
 * Screen size classes, the same idea as the web's breakpoints:
 * phone < 600 ≤ tablet < 1024 ≤ wide. `landscape` matters for the driver's tablet (list and map side by side).
 */
export function useLayout() {
  const { width, height } = useWindowDimensions();
  const size = width >= 1024 ? 'wide' : width >= 600 ? 'tablet' : 'phone';
  return {
    width,
    height,
    size,
    isPhone: size === 'phone',
    isTablet: size !== 'phone',
    landscape: width > height,
    /** Side gutter: 16 on phones, more on bigger screens. */
    gutter: size === 'phone' ? space.lg : size === 'tablet' ? space.xl : space.xxl,
  } as const;
}

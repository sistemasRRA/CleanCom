import type { ReactNode } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';
import { space, useLayout, useTheme } from '../theme/Theme';

type ScreenProps = {
  children: ReactNode;
  /** Content width limit on tablets so forms do not stretch across the screen. */
  maxWidth?: number;
  /** Centers the content vertically (sign in). */
  center?: boolean;
  /** false for screens that manage their own scroll (lists, maps). */
  scroll?: boolean;
};

/** Safe area, background, keyboard and responsive side gutters for every screen. */
export function Screen({ children, maxWidth = 640, center, scroll = true }: ScreenProps) {
  const { colors, scheme } = useTheme();
  const { gutter } = useLayout();
  const body = (
    <View style={{ width: '100%', maxWidth, alignSelf: 'center', paddingHorizontal: gutter, paddingVertical: space.xl, gap: space.lg }}>
      {children}
    </View>
  );
  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: colors.bg }} edges={['top', 'bottom', 'left', 'right']}>
      <StatusBar style={scheme === 'dark' ? 'light' : 'dark'} />
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        {scroll ? (
          <ScrollView
            contentContainerStyle={{ flexGrow: 1, justifyContent: center ? 'center' : 'flex-start' }}
            keyboardShouldPersistTaps="handled"
          >
            {body}
          </ScrollView>
        ) : (
          <View style={{ flex: 1, justifyContent: center ? 'center' : 'flex-start' }}>{body}</View>
        )}
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

import { useState, type ReactNode } from 'react';
import {
  ActivityIndicator,
  Image,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
  type StyleProp,
  type TextInputProps,
  type ViewStyle,
} from 'react-native';
import { font, radius, space, touch, useTheme } from '../theme/Theme';

const logos = {
  full: { light: require('../../assets/cleancom-logo.png'), dark: require('../../assets/cleancom-logo-light.png') },
  mark: { light: require('../../assets/cleancom-mark.png'), dark: require('../../assets/cleancom-mark-light.png') },
};

/**
 * CleanCom logo from cleancom.mx. `background` says what it sits on: 'auto' follows the theme,
 * 'light' forces black text (white card), 'dark' forces white text (navy bar).
 */
export function Logo({
  variant = 'full',
  height = 32,
  background = 'auto',
}: {
  variant?: 'full' | 'mark';
  height?: number;
  background?: 'auto' | 'light' | 'dark';
}) {
  const { scheme } = useTheme();
  // Sizes of the files in shared/assets (trimmed from the cleancom.mx logo).
  const ratio = variant === 'full' ? 488 / 108 : 132 / 108;
  const onDark = background === 'dark' || (background === 'auto' && scheme === 'dark');
  const source = logos[variant][onDark ? 'dark' : 'light'];
  return <Image source={source} style={{ height, width: height * ratio }} resizeMode="contain" accessibilityLabel="CleanCom" />;
}

export function Title({ children, size = 'xl' }: { children: ReactNode; size?: 'lg' | 'xl' | 'xxl' }) {
  const { colors } = useTheme();
  return <Text style={{ color: colors.text, fontSize: font.size[size], fontWeight: '700' }}>{children}</Text>;
}

export function Body({ children, muted, center }: { children: ReactNode; muted?: boolean; center?: boolean }) {
  const { colors } = useTheme();
  return (
    <Text style={{ color: muted ? colors.textMuted : colors.text, fontSize: font.size.md, lineHeight: 22, textAlign: center ? 'center' : 'left' }}>
      {children}
    </Text>
  );
}

type ButtonProps = {
  title: string;
  onPress: () => void;
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  busy?: boolean;
  disabled?: boolean;
  icon?: ReactNode;
  style?: StyleProp<ViewStyle>;
};

/** Big, easy-to-hit button (at least 48 points tall). */
export function Button({ title, onPress, variant = 'primary', busy, disabled, icon, style }: ButtonProps) {
  const { colors } = useTheme();
  const palette = {
    primary: { bg: colors.brand, fg: colors.onBrand, border: colors.brand },
    secondary: { bg: colors.surface, fg: colors.text, border: colors.border },
    danger: { bg: colors.surface, fg: colors.danger, border: colors.border },
    ghost: { bg: 'transparent', fg: colors.brand, border: 'transparent' },
  }[variant];
  const off = disabled || busy;
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: off, busy }}
      onPress={onPress}
      disabled={off}
      style={({ pressed }) => [
        styles.button,
        { backgroundColor: palette.bg, borderColor: palette.border, opacity: off ? 0.55 : pressed ? 0.85 : 1 },
        style,
      ]}
    >
      {busy ? <ActivityIndicator color={palette.fg} /> : icon}
      <Text style={{ color: palette.fg, fontSize: font.size.lg, fontWeight: '600' }}>{title}</Text>
    </Pressable>
  );
}

type FieldProps = TextInputProps & { label: string; hint?: string; error?: string; secret?: boolean };

export function TextField({ label, hint, error, secret, style, ...input }: FieldProps) {
  const { colors } = useTheme();
  const [hidden, setHidden] = useState(true);
  const [focused, setFocused] = useState(false);
  return (
    <View style={{ gap: space.xs }}>
      <Text style={{ color: colors.textMuted, fontSize: font.size.sm, fontWeight: '600' }}>{label}</Text>
      <View
        style={[
          styles.input,
          { backgroundColor: colors.surface, borderColor: error ? colors.danger : focused ? colors.brand : colors.border },
        ]}
      >
        <TextInput
          placeholderTextColor={colors.textMuted}
          secureTextEntry={secret && hidden}
          autoCapitalize="none"
          autoCorrect={false}
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
          style={[{ flex: 1, color: colors.text, fontSize: font.size.lg, minHeight: touch }, style]}
          {...input}
        />
        {secret && (
          <Pressable onPress={() => setHidden((h) => !h)} hitSlop={12} accessibilityRole="button">
            <Text style={{ color: colors.brand, fontWeight: '600' }}>{hidden ? 'Mostrar' : 'Ocultar'}</Text>
          </Pressable>
        )}
      </View>
      {(error || hint) && <Text style={{ color: error ? colors.danger : colors.textMuted, fontSize: font.size.sm }}>{error ?? hint}</Text>}
    </View>
  );
}

export function Alert({ children, tone = 'danger' }: { children: ReactNode; tone?: 'danger' | 'warning' | 'info' | 'success' }) {
  const { colors } = useTheme();
  const palette = {
    danger: [colors.dangerSoft, colors.danger],
    warning: [colors.warningSoft, colors.warning],
    info: [colors.brandSoft, colors.brand],
    success: [colors.successSoft, colors.success],
  }[tone];
  return (
    <View accessibilityRole="alert" style={{ backgroundColor: palette[0], padding: space.md, borderRadius: radius.sm }}>
      <Text style={{ color: palette[1], fontSize: font.size.md, lineHeight: 21 }}>{children}</Text>
    </View>
  );
}

export function Card({ children, style }: { children: ReactNode; style?: StyleProp<ViewStyle> }) {
  const { colors } = useTheme();
  return (
    <View style={[{ backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: radius.md, padding: space.lg, gap: space.md }, style]}>
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: touch,
    paddingHorizontal: space.xl,
    borderRadius: radius.md,
    borderWidth: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: space.sm,
  },
  input: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: space.sm,
    borderWidth: 1,
    borderRadius: radius.sm,
    paddingHorizontal: space.md,
  },
});

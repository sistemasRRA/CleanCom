import Ionicons from '@expo/vector-icons/Ionicons';
import type { ReactNode } from 'react';
import { Body, radius, space, useTheme } from '@cleancom/mobile-shared';
import { Pressable, View } from 'react-native';

/** A tappable row with an icon, a title, a line under it and a chevron. */
export function Row({
  icon,
  title,
  text,
  onPress,
  right,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  text?: string;
  onPress?: () => void;
  right?: ReactNode;
}) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={!onPress}
      accessibilityRole={onPress ? 'button' : undefined}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: space.md,
        padding: space.md,
        borderRadius: radius.md,
        backgroundColor: pressed ? colors.surface2 : colors.surface,
        borderWidth: 1,
        borderColor: colors.border,
      })}
    >
      <View style={{ width: 40, height: 40, borderRadius: 20, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}>
        <Ionicons name={icon} size={21} color={colors.brand} />
      </View>
      <View style={{ flex: 1, gap: 2 }}>
        <Body>{title}</Body>
        {text ? <Body muted>{text}</Body> : null}
      </View>
      {right ?? (onPress ? <Ionicons name="chevron-forward" size={20} color={colors.textMuted} /> : null)}
    </Pressable>
  );
}

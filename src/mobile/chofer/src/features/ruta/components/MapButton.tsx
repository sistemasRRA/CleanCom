import Ionicons from '@expo/vector-icons/Ionicons';
import { radius, touch, useTheme } from '@cleancom/mobile-shared';
import { Pressable } from 'react-native';

export function MapButton({
  icon,
  label,
  onPress,
  disabled,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  onPress: () => void;
  disabled?: boolean;
}) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityLabel={label}
      style={({ pressed }) => ({
        width: touch,
        height: touch,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: pressed ? colors.surface2 : colors.surface,
        borderWidth: 1,
        borderColor: colors.border,
        opacity: disabled ? 0.5 : 1,
      })}
    >
      <Ionicons name={icon} size={22} color={colors.brand} />
    </Pressable>
  );
}

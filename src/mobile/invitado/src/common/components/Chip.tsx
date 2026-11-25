import { radius, space, touch, useTheme } from '@cleancom/mobile-shared';
import { Pressable, Text } from 'react-native';

/** A choice among a few options (línea de servicio, turno, día). */
export function Chip({
  label,
  selected,
  onPress,
  disabled,
  note,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
  disabled?: boolean;
  note?: string;
}) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityState={{ selected, disabled }}
      style={({ pressed }) => ({
        minHeight: touch,
        paddingHorizontal: space.lg,
        paddingVertical: space.sm,
        borderRadius: radius.lg,
        borderWidth: 1.5,
        borderColor: selected ? colors.brand : colors.border,
        backgroundColor: selected ? colors.brandSoft : pressed ? colors.surface2 : colors.surface,
        justifyContent: 'center',
        opacity: disabled ? 0.45 : 1,
      })}
    >
      <Text style={{ color: selected ? colors.brand : colors.text, fontWeight: selected ? '700' : '500', fontSize: 15 }}>{label}</Text>
      {note && <Text style={{ color: colors.textMuted, fontSize: 12 }}>{note}</Text>}
    </Pressable>
  );
}

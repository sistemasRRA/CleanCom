import Ionicons from '@expo/vector-icons/Ionicons';
import { Pressable } from 'react-native';
import { radius, touch } from '@cleancom/mobile-shared';

export function HeaderButton({ icon, label, onPress }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void }) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={label}
      hitSlop={6}
      style={({ pressed }) => ({
        width: touch,
        height: touch,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: pressed ? 'rgba(255,255,255,0.18)' : 'rgba(255,255,255,0.08)',
        borderWidth: 1,
        borderColor: 'rgba(255,255,255,0.12)',
      })}
    >
      <Ionicons name={icon} size={22} color="#fff" />
    </Pressable>
  );
}

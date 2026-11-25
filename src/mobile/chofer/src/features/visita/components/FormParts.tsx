import { radius, space, touch, useTheme } from '@cleancom/mobile-shared';
import { Pressable, Text, TextInput, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';

export function Section({ title, children }: { title: string; children: React.ReactNode }) {
  const { colors } = useTheme();
  return (
    <View style={{ gap: space.sm }}>
      <Text style={{ color: colors.textMuted, fontWeight: '700', fontSize: 13, textTransform: 'uppercase', letterSpacing: 0.5 }}>{title}</Text>
      {children}
    </View>
  );
}

export function Choice({
  selected,
  label,
  icon,
  tone,
  small,
  onPress,
}: {
  selected: boolean;
  label: string;
  icon?: keyof typeof Ionicons.glyphMap;
  tone?: 'danger';
  small?: boolean;
  onPress: () => void;
}) {
  const { colors } = useTheme();
  const accent = tone === 'danger' ? colors.danger : colors.brand;
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityState={{ selected }}
      style={{
        flex: small ? undefined : 1,
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 6,
        minHeight: touch,
        paddingHorizontal: space.md,
        borderRadius: radius.md,
        borderWidth: selected ? 2 : 1,
        borderColor: selected ? accent : colors.border,
        backgroundColor: selected ? (tone === 'danger' ? colors.dangerSoft : colors.brandSoft) : colors.surface,
      }}
    >
      {icon && <Ionicons name={icon} size={20} color={selected ? accent : colors.textMuted} />}
      <Text style={{ color: selected ? accent : colors.text, fontWeight: '700' }}>{label}</Text>
    </Pressable>
  );
}

export function Stepper({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const { colors } = useTheme();
  const n = Number(value) || 0;
  const button = (icon: 'remove' | 'add', next: number) => (
    <Pressable
      onPress={() => onChange(String(Math.max(0, next)))}
      accessibilityRole="button"
      accessibilityLabel={icon === 'add' ? 'Una más' : 'Una menos'}
      style={{
        width: touch + 8,
        height: touch + 8,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: colors.surface,
      }}
    >
      <Ionicons name={icon} size={26} color={colors.brand} />
    </Pressable>
  );
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm }}>
      {button('remove', n - 1)}
      <TextInput
        value={value}
        onChangeText={(t) => onChange(t.replace(/\D/g, ''))}
        keyboardType="number-pad"
        style={{
          width: 90,
          height: touch + 8,
          textAlign: 'center',
          fontSize: 24,
          fontWeight: '700',
          color: colors.text,
          borderRadius: radius.md,
          borderWidth: 1,
          borderColor: colors.border,
          backgroundColor: colors.surface,
        }}
      />
      {button('add', n + 1)}
    </View>
  );
}

export function Input(props: React.ComponentProps<typeof TextInput>) {
  const { colors } = useTheme();
  return (
    <TextInput
      placeholderTextColor={colors.textMuted}
      {...props}
      style={{
        minHeight: touch,
        paddingHorizontal: space.md,
        paddingVertical: space.sm,
        fontSize: 16,
        color: colors.text,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: colors.surface,
        textAlignVertical: props.multiline ? 'top' : 'center',
      }}
    />
  );
}

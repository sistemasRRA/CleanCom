import { Body, space, useTheme } from '@cleancom/mobile-shared';
import { Switch, View } from 'react-native';

/** One notice preference: text, a hint below and its switch. */
export function PreferenceSwitch({ label, hint, value, onChange }: { label: string; hint: string; value: boolean; onChange: (v: boolean) => void }) {
  const { colors } = useTheme();
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.md }}>
      <View style={{ flex: 1, gap: space.xs }}>
        <Body>{label}</Body>
        <Body muted>{hint}</Body>
      </View>
      <Switch value={value} onValueChange={onChange} trackColor={{ true: colors.brand }} accessibilityLabel={label} />
    </View>
  );
}

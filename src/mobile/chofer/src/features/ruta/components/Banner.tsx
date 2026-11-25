import { radius, space, useTheme } from '@cleancom/mobile-shared';
import { Text, View } from 'react-native';

export function Banner({ children }: { children: string }) {
  const { colors } = useTheme();
  return (
    <View style={{ padding: space.md, borderRadius: radius.md, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border }}>
      <Text style={{ color: colors.textMuted, textAlign: 'center' }}>{children}</Text>
    </View>
  );
}

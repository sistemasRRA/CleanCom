import Ionicons from '@expo/vector-icons/Ionicons';
import type { ReactNode } from 'react';
import { Card, space, Title, useTheme } from '@cleancom/mobile-shared';
import { View } from 'react-native';

export function Panel({ title, icon, grow, children }: { title: string; icon: keyof typeof Ionicons.glyphMap; grow?: boolean; children: ReactNode }) {
  const { colors } = useTheme();
  return (
    <Card style={grow ? { flex: 1 } : undefined}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm }}>
        <Ionicons name={icon} size={20} color={colors.brand} />
        <Title size="lg">{title}</Title>
      </View>
      {children}
    </Card>
  );
}

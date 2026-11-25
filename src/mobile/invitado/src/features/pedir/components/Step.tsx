import type { ReactNode } from 'react';
import { Card, space, Title, useTheme } from '@cleancom/mobile-shared';
import { View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';

export function Step({ n, title, children }: { n: number; title: string; children: ReactNode }) {
  const { colors } = useTheme();
  return (
    <Card>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm }}>
        <View style={{ width: 28, height: 28, borderRadius: 14, backgroundColor: colors.brand, alignItems: 'center', justifyContent: 'center' }}>
          <Ionicons name={n === 1 ? 'location' : n === 2 ? 'shirt' : n === 3 ? 'calendar' : 'gift'} size={15} color={colors.onBrand} />
        </View>
        <Title size="lg">{title}</Title>
      </View>
      {children}
    </Card>
  );
}

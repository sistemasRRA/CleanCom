import { Body, space, useTheme } from '@cleancom/mobile-shared';
import Ionicons from '@expo/vector-icons/Ionicons';
import { View } from 'react-native';
import type { TrackingState } from '../../../offline/locationTracking';

/** Whether the office sees where the chofer is. Nothing while there is no shift to follow. */
export function TrackingNotice({ state }: { state: TrackingState }) {
  const { colors } = useTheme();
  if (state === 'off') return null;
  const notice: Record<Exclude<TrackingState, 'off'>, { icon: keyof typeof Ionicons.glyphMap; text: string; color: string }> = {
    on: { icon: 'navigate', text: 'Oficina ve tu ubicación durante el turno.', color: colors.brand },
    onlyOpen: {
      icon: 'warning',
      text: 'Oficina solo ve tu ubicación con la app abierta. En los ajustes del teléfono, permite la ubicación "Siempre" para CleanCom Chofer.',
      color: colors.warning,
    },
    denied: {
      icon: 'warning',
      text: 'Sin permiso de ubicación: oficina no ve dónde vas. Actívalo en los ajustes del teléfono.',
      color: colors.danger,
    },
  };
  const n = notice[state];
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm }} accessibilityRole="text">
      <Ionicons name={n.icon} size={18} color={n.color} />
      <View style={{ flex: 1 }}>
        <Body muted>{n.text}</Body>
      </View>
    </View>
  );
}

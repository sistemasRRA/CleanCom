import { radius, space, touch, useTheme } from '@cleancom/mobile-shared';
import { Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { done, type Parada, STATE } from '../types/mapa';
import { openNavigation } from '../../../common/ubicacion';

/** Card over the map with the tapped parada. */
export function Detalle({ parada: p, isNext, onClose }: { parada: Parada; isNext: boolean; onClose: () => void }) {
  const { colors } = useTheme();
  const name = p.invitadoName ?? p.viaje ?? `Parada ${p.order}`;
  return (
    <View
      style={{
        position: 'absolute',
        left: space.sm,
        right: space.sm,
        bottom: space.sm,
        padding: space.md,
        gap: space.xs,
        borderRadius: radius.md,
        backgroundColor: colors.surface,
        borderWidth: 1,
        borderColor: colors.border,
      }}
    >
      <View style={{ flexDirection: 'row', alignItems: 'flex-start', gap: space.sm }}>
        <View style={{ flex: 1 }}>
          <Text style={{ color: colors.text, fontWeight: '700', fontSize: 16 }} numberOfLines={1}>
            {p.order}. {name}
          </Text>
          <Text style={{ color: colors.textMuted, fontSize: 13 }}>
            {isNext ? 'Siguiente · ' : ''}
            {STATE[p.state]}
            {p.estimatedArrival ? ` · ${p.estimatedArrival.slice(0, 5)}` : ''}
          </Text>
        </View>
        <Pressable onPress={onClose} accessibilityRole="button" accessibilityLabel="Cerrar" hitSlop={10}>
          <Ionicons name="close" size={22} color={colors.textMuted} />
        </Pressable>
      </View>
      {p.address && (
        <Text style={{ color: colors.text }} numberOfLines={2}>
          {p.address}
        </Text>
      )}
      {!done(p) && (
        <Pressable
          onPress={() => openNavigation(name, p.lat != null && p.lng != null ? { lat: p.lat, lng: p.lng } : null, p.address ?? null)}
          accessibilityRole="button"
          style={({ pressed }) => ({
            marginTop: space.xs,
            minHeight: touch,
            borderRadius: radius.md,
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'center',
            gap: space.sm,
            backgroundColor: pressed ? colors.brandStrong : colors.brand,
          })}
        >
          <Ionicons name="navigate" size={18} color={colors.onBrand} />
          <Text style={{ color: colors.onBrand, fontWeight: '700' }}>Cómo llegar</Text>
        </Pressable>
      )}
    </View>
  );
}

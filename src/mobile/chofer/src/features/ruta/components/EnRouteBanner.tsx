import { Button, radius, space, useTheme } from '@cleancom/mobile-shared';
import { Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { KIND, type Parada, pointOf } from '../types/ruta';
import { openNavigation, useDistanceTo } from '../../../common/ubicacion';

/** While en camino: how far the parada is, and "Llegué" highlighted once inside the tolerance. */
export function EnRouteBanner({ parada: p, tolerance, busy, onArrive }: { parada: Parada; tolerance: number; busy: boolean; onArrive: () => void }) {
  const { colors } = useTheme();
  const distance = useDistanceTo(pointOf(p));
  const near = distance != null && distance <= tolerance;
  return (
    <View
      style={{
        gap: space.sm,
        padding: space.md,
        borderRadius: radius.md,
        backgroundColor: near ? colors.brand : colors.brandSoft,
      }}
    >
      <Text style={{ color: near ? colors.onBrand : colors.brand, fontWeight: '700', fontSize: 16 }}>
        {near ? `Ya estás a ${distance} m de ${p.invitadoName}` : `En camino a la parada ${p.order}: ${p.invitadoName}`}
      </Text>
      {!near && (
        <Text style={{ color: colors.brand }}>
          {distance != null ? `A ${distance >= 1000 ? `${(distance / 1000).toFixed(1)} km` : `${distance} m`}` : 'Sin ubicación del teléfono'}
          {p.estimatedArrival && ` · hora estimada ${p.estimatedArrival.slice(0, 5)}`}
        </Text>
      )}
      <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
        <Button
          title="Llegué"
          icon={<Ionicons name="flag" size={18} color={near ? colors.brand : colors.onBrand} />}
          busy={busy}
          onPress={onArrive}
          variant={near ? 'secondary' : 'primary'}
        />
        <Button title="Navegar" variant="ghost" onPress={() => openNavigation(p.invitadoName ?? KIND[p.kind], pointOf(p), p.address ?? null)} />
      </View>
    </View>
  );
}

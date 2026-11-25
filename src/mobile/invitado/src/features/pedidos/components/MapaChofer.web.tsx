import { radius, type Schemas, useTheme } from '@cleancom/mobile-shared';
import { View } from 'react-native';

type Chofer = Schemas['MyChoferDto'];

/**
 * Expo web (development only; the app ships to phones): react-native-maps has no web version, so the chofer is shown
 * with OpenStreetMap's embeddable map, framed on the chofer and the address.
 */
export function MapaChofer({ chofer }: { chofer: Chofer; old: boolean }) {
  const { colors } = useTheme();
  const points = [
    chofer.lat != null && chofer.lng != null ? [chofer.lat, chofer.lng] : null,
    chofer.destinationLat != null && chofer.destinationLng != null ? [chofer.destinationLat, chofer.destinationLng] : null,
  ].filter((p): p is number[] => p != null);
  if (points.length === 0) return null;
  const lats = points.map((p) => p[0]);
  const lngs = points.map((p) => p[1]);
  const pad = 0.006;
  const bbox = [Math.min(...lngs) - pad, Math.min(...lats) - pad, Math.max(...lngs) + pad, Math.max(...lats) + pad].join(',');
  const marker = points[0].join(',');
  const src = `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${marker}`;
  return (
    <View style={{ height: 280, borderRadius: radius.lg, overflow: 'hidden', borderWidth: 1, borderColor: colors.border }}>
      <iframe title="Tu chofer en el mapa" src={src} style={{ border: 0, width: '100%', height: '100%' }} />
    </View>
  );
}

import { Body, radius, useTheme } from '@cleancom/mobile-shared';
import { View } from 'react-native';

export type Punto = { lat: number; lng: number };

/**
 * Expo web (development only; the app ships to phones): react-native-maps has no web version, so the pin is shown on
 * OpenStreetMap's embeddable map. It cannot be dragged here; "Buscar en el mapa" and "Usar mi ubicación" still work.
 */
export function UbicacionMapa({ punto }: { punto: Punto | null; onChange: (p: Punto) => void }) {
  const { colors } = useTheme();
  if (!punto) return <Body muted>En la web de desarrollo el pin no se mueve con el dedo; usa los botones.</Body>;
  const pad = 0.004;
  const bbox = [punto.lng - pad, punto.lat - pad, punto.lng + pad, punto.lat + pad].join(',');
  const src = `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${punto.lat},${punto.lng}`;
  return (
    <View style={{ height: 260, borderRadius: radius.lg, overflow: 'hidden', borderWidth: 1, borderColor: colors.border }}>
      <iframe title="Tu dirección en el mapa" src={src} style={{ border: 0, width: '100%', height: '100%' }} />
    </View>
  );
}

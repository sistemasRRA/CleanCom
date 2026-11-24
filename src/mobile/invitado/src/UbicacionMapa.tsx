import { useEffect, useRef } from 'react';
import { View } from 'react-native';
import MapView, { Marker, type LatLng } from 'react-native-maps';
import Ionicons from '@expo/vector-icons/Ionicons';
import { radius, useTheme } from '@cleancom/mobile-shared';

export type Punto = { lat: number; lng: number };

/** Mexico City, when the address has no point yet. */
const CDMX: LatLng = { latitude: 19.4, longitude: -99.15 };

/**
 * The address's pin on the map (Apple Maps on iPhone, Google Maps on Android): tapping the map or dragging the pin
 * moves it. The view follows the pin when it comes from outside (search or "Usar mi ubicación").
 */
export function UbicacionMapa({ punto, onChange }: { punto: Punto | null; onChange: (p: Punto) => void }) {
  const { colors } = useTheme();
  const map = useRef<MapView>(null);
  const pin: LatLng | null = punto ? { latitude: punto.lat, longitude: punto.lng } : null;
  const key = punto ? `${punto.lat.toFixed(5)},${punto.lng.toFixed(5)}` : '';

  useEffect(() => {
    if (pin) map.current?.animateToRegion({ ...pin, latitudeDelta: 0.006, longitudeDelta: 0.006 });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  const move = (c: LatLng) => onChange({ lat: c.latitude, lng: c.longitude });
  return (
    <View style={{ height: 260, borderRadius: radius.lg, overflow: 'hidden', borderWidth: 1, borderColor: colors.border }}>
      <MapView
        ref={map}
        style={{ flex: 1 }}
        initialRegion={{ ...(pin ?? CDMX), latitudeDelta: pin ? 0.006 : 0.2, longitudeDelta: pin ? 0.006 : 0.2 }}
        onPress={(e) => move(e.nativeEvent.coordinate)}
        toolbarEnabled={false}
      >
        {pin && (
          <Marker coordinate={pin} draggable onDragEnd={(e) => move(e.nativeEvent.coordinate)} title="Tu dirección">
            <Ionicons name="location" size={40} color={colors.brand} />
          </Marker>
        )}
      </MapView>
    </View>
  );
}

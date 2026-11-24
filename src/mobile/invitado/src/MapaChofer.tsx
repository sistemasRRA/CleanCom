import { useEffect, useRef } from 'react';
import { View } from 'react-native';
import MapView, { Marker, type LatLng } from 'react-native-maps';
import Ionicons from '@expo/vector-icons/Ionicons';
import { radius, useTheme, type Schemas } from '@cleancom/mobile-shared';

type Chofer = Schemas['MyChoferDto'];

const EDGES = { top: 70, right: 70, bottom: 70, left: 70 };

/**
 * The chofer on the way and the invitado's address (Apple Maps on iPhone, Google Maps on Android). The view follows
 * both points as the chofer moves. Grey when the position is old (`old`).
 */
export function MapaChofer({ chofer, old }: { chofer: Chofer; old: boolean }) {
  const { colors } = useTheme();
  const map = useRef<MapView>(null);
  const car: LatLng | null = chofer.lat != null && chofer.lng != null ? { latitude: chofer.lat, longitude: chofer.lng } : null;
  const home: LatLng | null =
    chofer.destinationLat != null && chofer.destinationLng != null ? { latitude: chofer.destinationLat, longitude: chofer.destinationLng } : null;
  const points = [car, home].filter((p): p is LatLng => p != null);
  const key = points.map((p) => `${p.latitude.toFixed(4)},${p.longitude.toFixed(4)}`).join('|');

  useEffect(() => {
    if (!map.current || points.length === 0) return;
    if (points.length === 1) map.current.animateToRegion({ ...points[0], latitudeDelta: 0.02, longitudeDelta: 0.02 });
    else map.current.fitToCoordinates(points, { edgePadding: EDGES, animated: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  const color = old ? colors.textMuted : chofer.rutaColor || colors.brand;
  return (
    <View style={{ height: 280, borderRadius: radius.lg, overflow: 'hidden', borderWidth: 1, borderColor: colors.border }}>
      <MapView
        ref={map}
        style={{ flex: 1 }}
        initialRegion={{ ...(points[0] ?? { latitude: 19.4, longitude: -99.17 }), latitudeDelta: 0.05, longitudeDelta: 0.05 }}
        toolbarEnabled={false}
      >
        {home && (
          <Marker coordinate={home} title="Tu dirección" tracksViewChanges={false}>
            <View style={{ width: 34, height: 34, borderRadius: 17, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surface, borderWidth: 3, borderColor: colors.brand }}>
              <Ionicons name="home" size={16} color={colors.brand} />
            </View>
          </Marker>
        )}
        {car && (
          <Marker coordinate={car} title={chofer.name ?? 'Tu chofer'} tracksViewChanges={false} key={`${color}`}>
            <View style={{ width: 38, height: 38, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: color, borderWidth: 3, borderColor: '#fff' }}>
              <Ionicons name="car" size={18} color="#fff" />
            </View>
          </Marker>
        )}
      </MapView>
    </View>
  );
}

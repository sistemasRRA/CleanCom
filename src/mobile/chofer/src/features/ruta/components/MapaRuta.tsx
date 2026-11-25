import { Body, radius, space, useTheme } from '@cleancom/mobile-shared';
import { useEffect, useMemo, useRef, useState } from 'react';
import MapView, { Marker, Polyline } from 'react-native-maps';
import { Platform, View } from 'react-native';
import { CDMX, done, EDGES, type Parada, pointOf, type Ruta } from '../types/mapa';
import { Pin } from './MapaPin';
import { MapButton } from './MapButton';
import { Banner } from './Banner';
import { Detalle } from './ParadaDetalle';

/**
 * The day's paradas on a map, numbered in the order the API gave, joined in that order with a straight line
 * (the real path comes with the Google optimization). Tapping a pin shows the parada and "Cómo llegar".
 * Apple Maps on iPhone and Google Maps on Android; the driver's own position is the blue dot.
 */
export function MapaRuta({ ruta }: { ruta: Ruta | null }) {
  const { colors, scheme } = useTheme();
  const map = useRef<MapView>(null);
  const [ready, setReady] = useState(false);
  const [selected, setSelected] = useState<Parada | null>(null);

  const paradas = ruta?.paradas ?? [];
  const located = useMemo(() => paradas.filter((p) => pointOf(p)), [paradas]);
  const missing = paradas.length - located.length;
  const color = ruta?.ruta?.color || colors.brand;
  const nextId = ruta?.isToday ? ruta.nextParadaId : null;
  const coordinates = useMemo(() => located.map((p) => pointOf(p)!), [located]);
  // Refit only when the set of paradas changes, not on every state change, so the driver's zoom stays.
  const fitKey = `${ruta?.date}|${located.map((p) => p.id).join(',')}`;

  function fit() {
    if (!map.current || coordinates.length === 0) return;
    if (coordinates.length === 1) {
      map.current.animateToRegion({ ...coordinates[0], latitudeDelta: 0.02, longitudeDelta: 0.02 });
    } else {
      map.current.fitToCoordinates(coordinates, { edgePadding: EDGES, animated: true });
    }
  }

  useEffect(() => {
    if (ready) fit();
    setSelected(null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ready, fitKey]);

  // The selected parada follows the latest data (its state may change while it is open).
  const shown = selected ? (paradas.find((p) => p.id === selected.id) ?? null) : null;

  if (Platform.OS === 'web') {
    return (
      <Body muted center>
        El mapa está disponible en la app del teléfono o la tableta.
      </Body>
    );
  }

  return (
    <View style={{ gap: space.sm, flex: 1 }}>
      <View style={{ flex: 1, minHeight: 320, borderRadius: radius.md, overflow: 'hidden', backgroundColor: colors.surface2 }}>
        <MapView
          ref={map}
          style={{ flex: 1 }}
          initialRegion={CDMX}
          onMapReady={() => setReady(true)}
          onPress={() => setSelected(null)}
          showsUserLocation
          showsMyLocationButton={false}
          showsPointsOfInterests={false}
          toolbarEnabled={false}
          userInterfaceStyle={scheme}
        >
          {coordinates.length > 1 && <Polyline coordinates={coordinates} strokeColor={color} strokeWidth={3} lineDashPattern={[8, 6]} />}
          {located.map((p) => (
            <Marker
              key={p.id}
              coordinate={pointOf(p)!}
              onPress={(e) => {
                e.stopPropagation();
                setSelected(p);
              }}
              zIndex={p.id === nextId ? 10 : done(p) ? 1 : 5}
              anchor={{ x: 0.5, y: 0.5 }}
            >
              <Pin parada={p} color={color} isNext={p.id === nextId} />
            </Marker>
          ))}
        </MapView>

        <View style={{ position: 'absolute', top: space.sm, right: space.sm, gap: space.sm }}>
          <MapButton icon="scan-outline" label="Ver todas las paradas" onPress={fit} disabled={coordinates.length === 0} />
        </View>

        {located.length === 0 && (
          <View pointerEvents="none" style={{ position: 'absolute', left: space.md, right: space.md, bottom: space.md }}>
            <Banner>{paradas.length === 0 ? 'Sin paradas para mostrar en el mapa.' : 'Las paradas de este día no tienen ubicación.'}</Banner>
          </View>
        )}
        {shown && <Detalle parada={shown} isNext={shown.id === nextId} onClose={() => setSelected(null)} />}
      </View>
      {missing > 0 && located.length > 0 && (
        <Body muted>
          {missing === 1 ? '1 parada no tiene ubicación' : `${missing} paradas no tienen ubicación`} y no aparece en el mapa; búscala en la lista.
        </Body>
      )}
    </View>
  );
}

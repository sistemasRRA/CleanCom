import { useEffect, useMemo, useRef, useState } from 'react';
import { Platform, Pressable, Text, View } from 'react-native';
import MapView, { Marker, Polyline, type LatLng } from 'react-native-maps';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Body, radius, space, touch, useTheme, type Schemas } from '@cleancom/mobile-shared';
import { openNavigation } from './ubicacion';

type Ruta = Schemas['ChoferRutaDto'];
type Parada = Schemas['ChoferParadaDto'];

// CDMX, while there is nothing to show.
const CDMX = { latitude: 19.4, longitude: -99.17, latitudeDelta: 0.25, longitudeDelta: 0.25 };
const EDGES = { top: 60, right: 60, bottom: 60, left: 60 };

const done = (p: Parada) => p.state === 'Realizada' || p.state === 'NoRealizada';
const pointOf = (p: Parada): LatLng | null => (p.lat != null && p.lng != null ? { latitude: p.lat, longitude: p.lng } : null);

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
    return <Body muted center>El mapa está disponible en la app del teléfono o la tableta.</Body>;
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
        <Body muted>{missing === 1 ? '1 parada no tiene ubicación' : `${missing} paradas no tienen ubicación`} y no aparece en el mapa; búscala en la lista.</Body>
      )}
    </View>
  );
}

/** Numbered circle: ruta color while pending, bigger for the next one, muted when closed. */
function Pin({ parada: p, color, isNext }: { parada: Parada; color: string; isNext: boolean }) {
  const { colors } = useTheme();
  const size = isNext ? 38 : 30;
  const background = p.state === 'NoRealizada' ? colors.danger : done(p) ? colors.textMuted : p.state === 'Pendiente' ? color : colors.success;
  return (
    <View
      style={{
        width: size,
        height: size,
        borderRadius: size / 2,
        backgroundColor: background,
        borderWidth: isNext ? 3 : 2,
        borderColor: '#ffffff',
        alignItems: 'center',
        justifyContent: 'center',
        opacity: done(p) ? 0.75 : 1,
      }}
    >
      {done(p) ? (
        <Ionicons name={p.state === 'Realizada' ? 'checkmark' : 'close'} size={16} color="#ffffff" />
      ) : (
        <Text style={{ color: '#ffffff', fontWeight: '800', fontSize: isNext ? 16 : 14 }}>{p.order}</Text>
      )}
    </View>
  );
}

const STATE: Record<Schemas['ParadaState'], string> = {
  Pendiente: 'Pendiente',
  EnCamino: 'En camino',
  EnSitio: 'En sitio',
  Realizada: 'Realizada',
  NoRealizada: 'No realizada',
};

/** Card over the map with the tapped parada. */
function Detalle({ parada: p, isNext, onClose }: { parada: Parada; isNext: boolean; onClose: () => void }) {
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

function MapButton({ icon, label, onPress, disabled }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void; disabled?: boolean }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityLabel={label}
      style={({ pressed }) => ({
        width: touch,
        height: touch,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: pressed ? colors.surface2 : colors.surface,
        borderWidth: 1,
        borderColor: colors.border,
        opacity: disabled ? 0.5 : 1,
      })}
    >
      <Ionicons name={icon} size={22} color={colors.brand} />
    </Pressable>
  );
}

function Banner({ children }: { children: string }) {
  const { colors } = useTheme();
  return (
    <View style={{ padding: space.md, borderRadius: radius.md, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border }}>
      <Text style={{ color: colors.textMuted, textAlign: 'center' }}>{children}</Text>
    </View>
  );
}

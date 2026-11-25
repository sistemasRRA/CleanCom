import * as Location from 'expo-location';
import { useEffect, useState } from 'react';
import { Linking, Alert as RNAlert } from 'react-native';

export type Point = { lat: number; lng: number };

/**
 * Where the phone is now, or null without permission or signal. Only while the app is open: background location
 * during the shift arrives with the fleet (week 5).
 */
export async function currentPosition(): Promise<Point | null> {
  try {
    const { status } = await Location.requestForegroundPermissionsAsync();
    if (status !== 'granted') return null;
    const recent = await Location.getLastKnownPositionAsync({ maxAge: 30_000, requiredAccuracy: 100 });
    const position = recent ?? (await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced }));
    return { lat: position.coords.latitude, lng: position.coords.longitude };
  } catch {
    return null;
  }
}

/** Straight-line meters; only to suggest "Llegué". The API decides whether the arrival is within the tolerance. */
export function meters(a: Point, b: Point) {
  const rad = (d: number) => (d * Math.PI) / 180;
  const dLat = rad(b.lat - a.lat);
  const dLng = rad(b.lng - a.lng);
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(rad(a.lat)) * Math.cos(rad(b.lat)) * Math.sin(dLng / 2) ** 2;
  return Math.round(2 * 6_371_000 * Math.asin(Math.sqrt(h)));
}

/** Meters to `target` while it is set, updated as the chofer moves; null without permission. */
export function useDistanceTo(target: Point | null) {
  const lat = target?.lat;
  const lng = target?.lng;
  const key = lat == null || lng == null ? null : `${lat},${lng}`;
  // The distance is kept with the target it was measured to: a new target shows none until the next reading.
  const [measured, setMeasured] = useState<{ key: string; meters: number } | null>(null);
  const distance = measured && measured.key === key ? measured.meters : null;

  useEffect(() => {
    if (lat == null || lng == null) return;
    const target = `${lat},${lng}`;
    let subscription: Location.LocationSubscription | null = null;
    let cancelled = false;
    void (async () => {
      const { status } = await Location.requestForegroundPermissionsAsync();
      if (status !== 'granted' || cancelled) return;
      subscription = await Location.watchPositionAsync({ accuracy: Location.Accuracy.Balanced, distanceInterval: 25, timeInterval: 15_000 }, (p) =>
        setMeasured({ key: target, meters: meters({ lat: p.coords.latitude, lng: p.coords.longitude }, { lat, lng }) }),
      );
      if (cancelled) subscription.remove();
    })();
    return () => {
      cancelled = true;
      subscription?.remove();
    };
  }, [lat, lng]);

  return distance;
}

/** Asks Google Maps or Waze and opens it with the destination. */
export function openNavigation(name: string, point: Point | null, address: string | null) {
  const destination = point ? `${point.lat},${point.lng}` : (address ?? '');
  const google = `https://www.google.com/maps/dir/?api=1&travelmode=driving&destination=${encodeURIComponent(destination)}`;
  const waze = point
    ? `https://waze.com/ul?ll=${point.lat},${point.lng}&navigate=yes`
    : `https://waze.com/ul?q=${encodeURIComponent(destination)}&navigate=yes`;
  RNAlert.alert('¿Cómo quieres llegar?', name, [
    { text: 'Google Maps', onPress: () => void Linking.openURL(google) },
    { text: 'Waze', onPress: () => void Linking.openURL(waze) },
    { text: 'Cancelar', style: 'cancel' },
  ]);
}

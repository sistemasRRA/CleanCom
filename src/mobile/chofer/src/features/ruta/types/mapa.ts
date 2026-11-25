import type { Schemas } from '@cleancom/mobile-shared';
import type { LatLng } from 'react-native-maps';

export type Ruta = Schemas['ChoferRutaDto'];

export type Parada = Schemas['ChoferParadaDto'];

// CDMX, while there is nothing to show.
export const CDMX = { latitude: 19.4, longitude: -99.17, latitudeDelta: 0.25, longitudeDelta: 0.25 };

export const EDGES = { top: 60, right: 60, bottom: 60, left: 60 };

export const done = (p: Parada) => p.state === 'Realizada' || p.state === 'NoRealizada';

export const pointOf = (p: Parada): LatLng | null => (p.lat != null && p.lng != null ? { latitude: p.lat, longitude: p.lng } : null);

export const STATE: Record<Schemas['ParadaState'], string> = {
  Pendiente: 'Pendiente',
  EnCamino: 'En camino',
  EnSitio: 'En sitio',
  Realizada: 'Realizada',
  NoRealizada: 'No realizada',
};

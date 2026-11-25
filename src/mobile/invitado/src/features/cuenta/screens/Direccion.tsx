import { Alert, Body, Button, Card, type Schemas, Screen, space, TextField, Title } from '@cleancom/mobile-shared';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import * as Location from 'expo-location';
import { View } from 'react-native';
import { useLoad } from '../../../common/hooks/useLoad';
import { type Punto, UbicacionMapa } from '../components/UbicacionMapa';
import { Chip } from '../../../common/components/Chip';
import { cuentaApi } from '../services/cuentaApi';

const PRECISION: Record<NonNullable<Schemas['GeocodePrecision']>, string> = {
  Direccion: 'Encontramos tu dirección. Revisa que el pin esté en tu casa.',
  Calle: 'Encontramos la calle, pero no el número. Mueve el pin a tu casa.',
  Colonia: 'Solo encontramos la colonia. Mueve el pin a tu casa.',
  CodigoPostal: 'Solo encontramos el código postal. Mueve el pin a tu casa.',
};

/**
 * Add or edit an address (POST|PUT /yo/direcciones). The postal code brings its colonias and alcaldía from the
 * official catalog. The pin on the map comes from the typed address (GET /direcciones/ubicar, OpenStreetMap), from the
 * phone's location or from a tap on the map; with it the API locates the zona and the chofer has where to go.
 * Google autocomplete arrives with the keys.
 * `?id=` edits that address; `?volver=pedir` goes back to asking for a service.
 */
export function Direccion() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  const editing = id ? Number(id) : null;
  const me = useLoad(() => cuentaApi.me(), []);
  const [label, setLabel] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [neighborhood, setNeighborhood] = useState<string | null>(null);
  const [borough, setBorough] = useState<string | null>(null);
  const [street, setStreet] = useState('');
  const [number, setNumber] = useState('');
  const [interior, setInterior] = useState('');
  const [references, setReferences] = useState('');
  const [punto, setPunto] = useState<Punto | null>(null);
  const [mapNote, setMapNote] = useState<string | null>(null);
  const [locating, setLocating] = useState<'buscar' | 'gps' | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const a = editing ? me.data?.addresses.find((x) => x.id === editing) : null;
    if (!a) return;
    setLabel(a.label ?? '');
    setPostalCode(a.postalCode);
    setNeighborhood(a.neighborhood);
    setBorough(a.borough);
    setStreet(a.street);
    setNumber(a.exteriorNumber ?? '');
    setInterior(a.interiorNumber ?? '');
    setReferences(a.references ?? '');
    setPunto(a.lat != null && a.lng != null ? { lat: a.lat, lng: a.lng } : null);
  }, [me.data, editing]);

  const cp = postalCode.replace(/\D/g, '');
  const colonias = useLoad(async () => (cp.length === 5 ? await cuentaApi.postalCode(cp) : []), [cp]);
  const options = colonias.data ?? [];
  useEffect(() => {
    if (options.length > 0 && !borough) setBorough(options[0].borough);
    if (options.length === 1 && !neighborhood) setNeighborhood(options[0].settlement);
  }, [options, borough, neighborhood]);

  /** The typed address on the map (OpenStreetMap, through the API). */
  async function search() {
    setLocating('buscar');
    setMapNote(null);
    try {
      const r = await cuentaApi.locate({
        street: street.trim(),
        exteriorNumber: number.trim() || undefined,
        neighborhood: neighborhood ?? undefined,
        borough: borough ?? undefined,
        postalCode: cp,
      });
      if (r.found && r.lat != null && r.lng != null) {
        setPunto({ lat: r.lat, lng: r.lng });
        setMapNote(r.precision ? PRECISION[r.precision] : PRECISION.Direccion);
      } else {
        setMapNote('No encontramos tu dirección en el mapa. Usa tu ubicación o toca el mapa donde está tu casa.');
      }
    } catch (e) {
      setMapNote((e as Error).message);
    } finally {
      setLocating(null);
    }
  }

  /** The phone's location, asking for permission only when the invitado taps the button. */
  async function locateMe() {
    setLocating('gps');
    setMapNote(null);
    try {
      const permission = await Location.requestForegroundPermissionsAsync();
      if (!permission.granted) {
        setMapNote('Sin permiso de ubicación. Puedes buscar tu dirección o tocar el mapa donde está tu casa.');
        return;
      }
      const position = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.High });
      setPunto({ lat: position.coords.latitude, lng: position.coords.longitude });
      setMapNote('Usamos la ubicación de tu teléfono. Si no estás en esa dirección, mueve el pin a tu casa.');
    } catch {
      setMapNote('No pudimos obtener tu ubicación. Busca tu dirección o toca el mapa donde está tu casa.');
    } finally {
      setLocating(null);
    }
  }

  async function save() {
    setBusy(true);
    setError(null);
    const body = {
      label: label.trim() || null,
      street: street.trim(),
      exteriorNumber: number.trim() || null,
      interiorNumber: interior.trim() || null,
      postalCode: cp,
      neighborhood,
      borough,
      references: references.trim() || null,
      lat: punto?.lat ?? null,
      lng: punto?.lng ?? null,
      zonaId: null,
    };
    try {
      if (editing) await cuentaApi.updateDireccion(editing, body);
      else await cuentaApi.addDireccion(body);
      router.back();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <Screen maxWidth={560}>
      <View style={{ gap: space.xs }}>
        <Title size="xxl">{editing ? 'Editar dirección' : 'Nueva dirección'}</Title>
        <Body muted>Donde pasamos por tus prendas y las entregamos.</Body>
      </View>
      <Card>
        <TextField label="Nombre (opcional)" placeholder="Casa, Oficina…" value={label} onChangeText={setLabel} maxLength={50} />
        <TextField
          label="Código postal"
          placeholder="03340"
          keyboardType="number-pad"
          maxLength={5}
          value={postalCode}
          onChangeText={(t) => {
            setPostalCode(t);
            setNeighborhood(null);
            setBorough(null);
          }}
          error={cp.length === 5 && !colonias.loading && options.length === 0 ? 'No encontramos ese código postal.' : undefined}
        />
        {options.length > 0 && (
          <View style={{ gap: space.sm }}>
            <Body>Colonia{borough ? ` · ${borough}` : ''}</Body>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
              {options.map((o) => (
                <Chip
                  key={`${o.settlement}-${o.settlementType}`}
                  label={o.settlement}
                  selected={neighborhood === o.settlement}
                  onPress={() => (setNeighborhood(o.settlement), setBorough(o.borough))}
                />
              ))}
            </View>
          </View>
        )}
        <TextField
          label="Calle"
          placeholder="Calz. de Tlalpan"
          value={street}
          onChangeText={setStreet}
          maxLength={200}
          autoComplete="street-address"
        />
        <View style={{ flexDirection: 'row', gap: space.md }}>
          <View style={{ flex: 1 }}>
            <TextField label="Número" placeholder="1200" value={number} onChangeText={setNumber} maxLength={20} />
          </View>
          <View style={{ flex: 1 }}>
            <TextField label="Interior (opcional)" placeholder="4B" value={interior} onChangeText={setInterior} maxLength={20} />
          </View>
        </View>
        <TextField
          label="Referencias (opcional)"
          placeholder="Portón negro, entre…"
          value={references}
          onChangeText={setReferences}
          maxLength={500}
          multiline
        />
      </Card>
      <Card>
        <Title size="lg">¿Dónde está en el mapa?</Title>
        <Body muted>Así el chofer llega directo y puedes ver cuánto le falta. Toca el mapa o arrastra el pin para ajustarlo.</Body>
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
          <View style={{ flexGrow: 1, flexBasis: 160 }}>
            <Button
              title="Buscar mi dirección"
              variant="secondary"
              busy={locating === 'buscar'}
              disabled={locating != null || cp.length !== 5 || street.trim().length < 2}
              onPress={() => void search()}
            />
          </View>
          <View style={{ flexGrow: 1, flexBasis: 160 }}>
            <Button
              title="Usar mi ubicación"
              variant="secondary"
              busy={locating === 'gps'}
              disabled={locating != null}
              onPress={() => void locateMe()}
            />
          </View>
        </View>
        {mapNote && <Body muted>{mapNote}</Body>}
        <UbicacionMapa
          punto={punto}
          onChange={(p) => {
            setPunto(p);
            setMapNote('Listo: guardaremos tu casa donde dejaste el pin.');
          }}
        />
      </Card>
      {error && <Alert>{error}</Alert>}
      <Button
        title="Guardar dirección"
        busy={busy}
        disabled={cp.length !== 5 || !neighborhood || street.trim().length < 2}
        onPress={() => void save()}
      />
      <Button title="Cancelar" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

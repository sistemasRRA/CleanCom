import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { Alert, Body, Button, Card, Screen, TextField, Title, api, space, unwrap } from '@cleancom/mobile-shared';
import { Chip, useLoad } from '../ui';

/**
 * Add or edit an address (POST|PUT /yo/direcciones). The postal code brings its colonias and alcaldía from the
 * official catalog; the API locates the zona when it is saved. Google autocomplete arrives with the keys.
 * `?id=` edits that address; `?volver=pedir` goes back to asking for a service.
 */
export default function Direccion() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  const editing = id ? Number(id) : null;
  const me = useLoad(() => unwrap(api.GET('/yo')), []);
  const [label, setLabel] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [neighborhood, setNeighborhood] = useState<string | null>(null);
  const [borough, setBorough] = useState<string | null>(null);
  const [street, setStreet] = useState('');
  const [number, setNumber] = useState('');
  const [interior, setInterior] = useState('');
  const [references, setReferences] = useState('');
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
  }, [me.data, editing]);

  const cp = postalCode.replace(/\D/g, '');
  const colonias = useLoad(async () => (cp.length === 5 ? await unwrap(api.GET('/codigos-postales/{code}', { params: { path: { code: cp } } })) : []), [cp]);
  const options = colonias.data ?? [];
  useEffect(() => {
    if (options.length > 0 && !borough) setBorough(options[0].borough);
    if (options.length === 1 && !neighborhood) setNeighborhood(options[0].settlement);
  }, [options, borough, neighborhood]);

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
      lat: null,
      lng: null,
      zonaId: null,
    };
    try {
      if (editing) await unwrap(api.PUT('/yo/direcciones/{addressId}', { params: { path: { addressId: editing } }, body }));
      else await unwrap(api.POST('/yo/direcciones', { body }));
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
                <Chip key={`${o.settlement}-${o.settlementType}`} label={o.settlement} selected={neighborhood === o.settlement} onPress={() => (setNeighborhood(o.settlement), setBorough(o.borough))} />
              ))}
            </View>
          </View>
        )}
        <TextField label="Calle" placeholder="Calz. de Tlalpan" value={street} onChangeText={setStreet} maxLength={200} autoComplete="street-address" />
        <View style={{ flexDirection: 'row', gap: space.md }}>
          <View style={{ flex: 1 }}>
            <TextField label="Número" placeholder="1200" value={number} onChangeText={setNumber} maxLength={20} />
          </View>
          <View style={{ flex: 1 }}>
            <TextField label="Interior (opcional)" placeholder="4B" value={interior} onChangeText={setInterior} maxLength={20} />
          </View>
        </View>
        <TextField label="Referencias (opcional)" placeholder="Portón negro, entre…" value={references} onChangeText={setReferences} maxLength={500} multiline />
      </Card>
      {error && <Alert>{error}</Alert>}
      <Button title="Guardar dirección" busy={busy} disabled={cp.length !== 5 || !neighborhood || street.trim().length < 2} onPress={() => void save()} />
      <Button title="Cancelar" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

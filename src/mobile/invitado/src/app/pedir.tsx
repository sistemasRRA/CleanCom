import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, Body, Button, Card, Screen, TextField, Title, api, space, unwrap, useTheme, type Schemas } from '@cleancom/mobile-shared';
import { Chip, Row, TURNOS, addressLine, day, useLoad } from '../ui';

type Availability = Schemas['AvailabilityDto'];

/**
 * Pedir servicio (docs/pantallas/apps/invitado.md): address, service, day and turno, confirm. The day comes from the
 * same agenda engine as the office (POST /agenda/disponibilidad and /agenda/agendar), with the Invitado's rules.
 * Benefits arrive in week 6.
 */
export default function Pedir() {
  const { colors } = useTheme();
  const me = useLoad(() => unwrap(api.GET('/yo')), []);
  const lineas = useLoad(() => unwrap(api.GET('/lineas-servicio')), []);
  // Back from adding an address: show it.
  useFocusEffect(
    useCallback(() => {
      void me.reload();
    }, [me.reload]),
  );

  const [addressId, setAddressId] = useState<number | null>(null);
  const [selectedLineas, setSelectedLineas] = useState<number[]>([]);
  const [garments, setGarments] = useState('');
  const [notes, setNotes] = useState('');
  const [turno, setTurno] = useState<Schemas['Turno']>('CualquierHora');
  const [availability, setAvailability] = useState<Availability | null>(null);
  const [date, setDate] = useState<string | null>(null);
  const [loadingDays, setLoadingDays] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState<{ message: string; pedidoId: number | null } | null>(null);

  const addresses = me.data?.addresses ?? [];
  // The only (or newest) address is chosen for them.
  useEffect(() => {
    if (addressId == null && addresses.length > 0) setAddressId(addresses[addresses.length - 1].id);
  }, [addresses, addressId]);

  // Ask the engine for the first day with cupo whenever the address or the turno changes.
  useEffect(() => {
    if (addressId == null) return;
    let cancelled = false;
    setLoadingDays(true);
    setError(null);
    unwrap(api.POST('/agenda/disponibilidad', { body: { addressId, lat: null, lng: null, postalCode: null, neighborhood: null, zonaId: null, turno } }))
      .then((a) => {
        if (cancelled) return;
        setAvailability(a);
        setDate(a.date);
      })
      .catch((e: Error) => !cancelled && setError(e.message))
      .finally(() => !cancelled && setLoadingDays(false));
    return () => {
      cancelled = true;
    };
  }, [addressId, turno]);

  async function confirm() {
    if (!me.data || addressId == null) return;
    setBusy(true);
    setError(null);
    try {
      const r = await unwrap(
        api.POST('/agenda/agendar', {
          body: {
            invitadoId: me.data.id,
            addressId,
            turno,
            specificTime: null,
            priority: false,
            garmentsApprox: garments.trim() ? Number(garments) : null,
            driverNotes: notes.trim() || null,
            lineaServicioIds: selectedLineas,
            registeredAt: null,
            date,
          },
        }),
      );
      if (r.scheduled) {
        setDone({ message: r.message, pedidoId: r.pedidoId });
      } else {
        // The day filled up meanwhile: the engine proposes the next one.
        setAvailability(r.availability);
        setDate(r.availability.date);
        setError(r.message);
      }
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <Screen center maxWidth={480}>
        <View style={{ alignItems: 'center', gap: space.md }}>
          <View style={{ width: 72, height: 72, borderRadius: 36, backgroundColor: colors.successSoft, alignItems: 'center', justifyContent: 'center' }}>
            <Ionicons name="checkmark" size={40} color={colors.success} />
          </View>
          <Title size="xl">¡Listo!</Title>
          <Body center>{done.message}</Body>
          <Body muted center>
            Te avisaremos cuando el chofer vaya en camino.
          </Body>
        </View>
        {done.pedidoId != null && (
          <Button title="Ver mi pedido" onPress={() => router.replace({ pathname: '/pedido/[id]', params: { id: String(done.pedidoId) } })} />
        )}
        <Button title="Volver al inicio" variant="ghost" onPress={() => router.back()} />
      </Screen>
    );
  }

  const outOfCoverage = availability?.outOfCoverage ?? false;
  const otherDays = (availability?.days ?? []).filter((d) => d.available).slice(0, 8);
  const canConfirm = addressId != null && !!date && !outOfCoverage && !loadingDays;

  return (
    <Screen maxWidth={640}>
      <Button title="Regresar" variant="ghost" onPress={() => router.back()} icon={<Ionicons name="chevron-back" size={20} color={colors.brand} />} />
      <Title size="xxl">Pedir servicio</Title>
      {me.error && <Alert>{me.error}</Alert>}

      <Step n={1} title="¿Dónde pasamos?">
        {addresses.map((a) => (
          <Row
            key={a.id}
            icon={addressId === a.id ? 'radio-button-on' : 'radio-button-off'}
            title={a.label || addressLine(a)}
            text={a.label ? addressLine(a) : (a.borough ?? undefined)}
            onPress={() => setAddressId(a.id)}
            right={<View />}
          />
        ))}
        <Button title={addresses.length ? 'Otra dirección' : 'Agregar mi dirección'} variant="secondary" onPress={() => router.push('/direccion')} />
      </Step>

      <Step n={2} title="¿Qué servicio?">
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
          {(lineas.data ?? []).filter((l) => l.active).map((l) => (
            <Chip
              key={l.id}
              label={l.name}
              selected={selectedLineas.includes(l.id)}
              onPress={() => setSelectedLineas((s) => (s.includes(l.id) ? s.filter((x) => x !== l.id) : [...s, l.id]))}
            />
          ))}
        </View>
        <TextField label="Prendas aproximadas (opcional)" placeholder="10" keyboardType="number-pad" maxLength={3} value={garments} onChangeText={(t) => setGarments(t.replace(/\D/g, ''))} />
        <TextField label="Notas para el chofer (opcional)" placeholder="Tocar el timbre, dejar en recepción…" value={notes} onChangeText={setNotes} maxLength={500} multiline />
      </Step>

      <Step n={3} title="¿Cuándo?">
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
          {TURNOS.map((t) => (
            <Chip key={t.key} label={t.label} note={t.note} selected={turno === t.key} onPress={() => setTurno(t.key)} />
          ))}
        </View>
        {addressId == null && <Body muted>Elige primero la dirección.</Body>}
        {loadingDays && <Body muted>Buscando el primer día disponible de tu zona…</Body>}
        {!loadingDays && outOfCoverage && (
          <Alert tone="warning">Tu dirección está fuera de nuestra zona de servicio por ahora. Escríbenos por WhatsApp y vemos cómo ayudarte.</Alert>
        )}
        {!loadingDays && availability && !outOfCoverage && (
          <>
            {availability.date ? (
              <Body>{`Primer día disponible: ${day(availability.date, true)}.`}</Body>
            ) : (
              <Alert tone="warning">No hay días con lugar en las próximas semanas. Escríbenos por WhatsApp.</Alert>
            )}
            {otherDays.length > 1 && (
              <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
                {otherDays.map((d) => (
                  <Chip key={d.date} label={day(d.date)} selected={date === d.date} onPress={() => setDate(d.date)} />
                ))}
              </View>
            )}
          </>
        )}
      </Step>

      <Step n={4} title="Beneficios">
        <Body muted>Tus sellos y cupones se podrán aplicar aquí muy pronto.</Body>
      </Step>

      {error && <Alert>{error}</Alert>}
      <Button title={date ? `Confirmar · ${day(date)}` : 'Confirmar'} busy={busy} disabled={!canConfirm} onPress={() => void confirm()} />
    </Screen>
  );
}

function Step({ n, title, children }: { n: number; title: string; children: ReactNode }) {
  const { colors } = useTheme();
  return (
    <Card>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm }}>
        <View style={{ width: 28, height: 28, borderRadius: 14, backgroundColor: colors.brand, alignItems: 'center', justifyContent: 'center' }}>
          <Ionicons name={n === 1 ? 'location' : n === 2 ? 'shirt' : n === 3 ? 'calendar' : 'gift'} size={15} color={colors.onBrand} />
        </View>
        <Title size="lg">{title}</Title>
      </View>
      {children}
    </Card>
  );
}

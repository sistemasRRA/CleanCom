import { Alert, Body, Button, type Schemas, Screen, space, TextField, Title, useTheme } from '@cleancom/mobile-shared';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { useLoad } from '../../../common/hooks/useLoad';
import { Step } from '../components/Step';
import { Row } from '../../../common/components/Row';
import { addressLine, day, TURNOS } from '../../../common/format';
import { Chip } from '../../../common/components/Chip';
import { pedirApi } from '../services/pedirApi';
import { beneficiosApi } from '../../beneficios';
import { cuentaApi } from '../../cuenta';

type Availability = Schemas['AvailabilityDto'];

/**
 * Pedir servicio (docs/pantallas/apps/invitado.md): address, service, day and turno, confirm. The day comes from the
 * same agenda engine as the office (POST /agenda/disponibilidad and /agenda/agendar), with the Invitado's rules.
 * Beneficios: the available sellos and cupones of the invitado (GET /yo/beneficios); the API checks línea, validity and
 * combining when confirming, and reserves them for the pedido.
 */
export function Pedir() {
  const { colors } = useTheme();
  // Back from adding an address: show it.
  const me = useLoad(() => cuentaApi.me(), [], { reloadOnFocus: true });
  const lineas = useLoad(() => pedirApi.lineasServicio(), []);
  const beneficios = useLoad(() => beneficiosApi.mine(), []);

  const [addressId, setAddressId] = useState<number | null>(null);
  const [selectedLineas, setSelectedLineas] = useState<number[]>([]);
  const [garments, setGarments] = useState('');
  const [notes, setNotes] = useState('');
  const [turno, setTurno] = useState<Schemas['Turno']>('CualquierHora');
  const [beneficioIds, setBeneficioIds] = useState<number[]>([]);
  const [availability, setAvailability] = useState<Availability | null>(null);
  const [date, setDate] = useState<string | null>(null);
  // Address and turno of the last answer of the agenda engine: until the current ones answer, it is looking for a day.
  const [answeredKey, setAnsweredKey] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState<{ message: string; pedidoId: number | null } | null>(null);

  const addresses = me.data?.addresses ?? [];
  // The only (or newest) address is chosen for them.
  const chosenId = addressId ?? addresses[addresses.length - 1]?.id ?? null;

  // Ask the engine for the first day with cupo whenever the address (or its data, after editing it) or the turno changes.
  const daysKey = chosenId == null ? null : `${JSON.stringify(addresses.find((a) => a.id === chosenId))}|${turno}`;
  const loadingDays = daysKey != null && answeredKey !== daysKey;
  useEffect(() => {
    if (chosenId == null || daysKey == null) return;
    let cancelled = false;
    pedirApi
      .availability({ addressId: chosenId, lat: null, lng: null, postalCode: null, neighborhood: null, zonaId: null, turno })
      .then((a) => {
        if (cancelled) return;
        setAvailability(a);
        setDate(a.date);
        setError(null);
      })
      .catch((e: Error) => !cancelled && setError(e.message))
      .finally(() => !cancelled && setAnsweredKey(daysKey));
    return () => {
      cancelled = true;
    };
  }, [chosenId, daysKey, turno]);

  async function confirm() {
    if (!me.data || chosenId == null) return;
    setBusy(true);
    setError(null);
    try {
      const r = await pedirApi.schedule({
        invitadoId: me.data.id,
        addressId: chosenId,
        turno,
        specificTime: null,
        priority: false,
        garmentsApprox: garments.trim() ? Number(garments) : null,
        driverNotes: notes.trim() || null,
        lineaServicioIds: selectedLineas,
        registeredAt: null,
        date,
        beneficioIds,
      });
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
          <View
            style={{ width: 72, height: 72, borderRadius: 36, backgroundColor: colors.successSoft, alignItems: 'center', justifyContent: 'center' }}
          >
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

  const available = (beneficios.data ?? []).filter((b) => b.state === 'Disponible');
  const outOfCoverage = availability?.outOfCoverage ?? false;
  const otherDays = (availability?.days ?? []).filter((d) => d.available).slice(0, 8);
  const canConfirm = chosenId != null && !!date && !outOfCoverage && !loadingDays;

  return (
    <Screen maxWidth={640}>
      <Button title="Regresar" variant="ghost" onPress={() => router.back()} icon={<Ionicons name="chevron-back" size={20} color={colors.brand} />} />
      <Title size="xxl">Pedir servicio</Title>
      {me.error && <Alert>{me.error}</Alert>}

      <Step n={1} title="¿Dónde pasamos?">
        {addresses.map((a) => (
          <Row
            key={a.id}
            icon={chosenId === a.id ? 'radio-button-on' : 'radio-button-off'}
            title={a.label || addressLine(a)}
            text={a.label ? addressLine(a) : (a.borough ?? undefined)}
            onPress={() => setAddressId(a.id)}
            right={
              <Pressable
                accessibilityRole="button"
                accessibilityLabel="Editar dirección"
                hitSlop={8}
                onPress={() => router.push({ pathname: '/direccion', params: { id: String(a.id) } })}
              >
                <Ionicons name="create-outline" size={20} color={colors.brand} />
              </Pressable>
            }
          />
        ))}
        <Button title={addresses.length ? 'Otra dirección' : 'Agregar mi dirección'} variant="secondary" onPress={() => router.push('/direccion')} />
      </Step>

      <Step n={2} title="¿Qué servicio?">
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
          {(lineas.data ?? [])
            .filter((l) => l.active)
            .map((l) => (
              <Chip
                key={l.id}
                label={l.name}
                selected={selectedLineas.includes(l.id)}
                onPress={() => setSelectedLineas((s) => (s.includes(l.id) ? s.filter((x) => x !== l.id) : [...s, l.id]))}
              />
            ))}
        </View>
        <TextField
          label="Prendas aproximadas (opcional)"
          placeholder="10"
          keyboardType="number-pad"
          maxLength={3}
          value={garments}
          onChangeText={(t) => setGarments(t.replace(/\D/g, ''))}
        />
        <TextField
          label="Notas para el chofer (opcional)"
          placeholder="Tocar el timbre, dejar en recepción…"
          value={notes}
          onChangeText={setNotes}
          maxLength={500}
          multiline
        />
      </Step>

      <Step n={3} title="¿Cuándo?">
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: space.sm }}>
          {TURNOS.map((t) => (
            <Chip key={t.key} label={t.label} note={t.note} selected={turno === t.key} onPress={() => setTurno(t.key)} />
          ))}
        </View>
        {chosenId == null && <Body muted>Elige primero la dirección.</Body>}
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
        {available.length === 0 ? (
          <Body muted>No tienes sellos ni cupones disponibles por ahora.</Body>
        ) : (
          <>
            <Body muted>Elige los que quieras usar; los aplicamos en tu ticket.</Body>
            {available.map((b) => {
              const on = beneficioIds.includes(b.id);
              // A cupón of a línea not chosen yet adds that línea to the service.
              const missing = b.lineaServicioId != null && !selectedLineas.includes(b.lineaServicioId);
              return (
                <Row
                  key={b.id}
                  icon={on ? 'checkbox' : 'square-outline'}
                  title={b.name}
                  text={`${b.mechanic} · ${b.expiresOn ? `vence el ${day(b.expiresOn)}` : 'vale hasta la entrega de tu pedido'}${missing && b.lineaName ? ` · se agrega ${b.lineaName}` : ''}`}
                  onPress={() => {
                    setBeneficioIds((ids) => (on ? ids.filter((x) => x !== b.id) : [...ids, b.id]));
                    if (!on && missing) setSelectedLineas((ls) => [...ls, b.lineaServicioId!]);
                  }}
                  right={<View />}
                />
              );
            })}
          </>
        )}
      </Step>

      {error && <Alert>{error}</Alert>}
      <Button title={date ? `Confirmar · ${day(date)}` : 'Confirmar'} busy={busy} disabled={!canConfirm} onPress={() => void confirm()} />
    </Screen>
  );
}

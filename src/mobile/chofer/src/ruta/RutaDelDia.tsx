import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Alert as RNAlert, Linking, Pressable, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Alert, ApiError, Body, Button, Card, Title, api, radius, space, touch, unwrap, useTheme, type Schemas } from '@cleancom/mobile-shared';
import { currentPosition, openNavigation, useDistanceTo } from './ubicacion';
import { CerrarVisita } from './CerrarVisita';
import { ACTION_NAME, discard, flush, newLocalId, onRuta, saveRuta, savedRuta, submit, useQueue, withPending, type Action } from '../offline/cola';

type Ruta = Schemas['ChoferRutaDto'];
type Parada = Schemas['ChoferParadaDto'];

const KIND: Record<Schemas['ParadaKind'], string> = {
  Recoleccion: 'Recolección',
  Entrega: 'Entrega',
  RecoleccionYEntrega: 'Recolección y entrega',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
  Salida: 'Salida',
  Regreso: 'Regreso',
  Comida: 'Comida',
  Traslado: 'Traslado',
};
const CATEGORY: Record<NonNullable<Schemas['ParadaCategory']>, string> = {
  ClienteFinal: 'Cliente final',
  Convenio: 'Convenio',
  Insumos: 'Insumos',
  Traspaso: 'Traspaso',
};
const TURNO: Record<Schemas['Turno'], string> = {
  Manana: 'Mañana',
  Tarde: 'Tarde',
  CualquierHora: 'Cualquier hora',
  HoraEspecifica: 'Hora específica',
};

/** yyyy-MM-dd plus n days, without time zone shifts. */
function addDays(iso: string, n: number) {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d + n)).toISOString().slice(0, 10);
}

const clock = new Intl.DateTimeFormat('es-MX', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'America/Mexico_City' });
const at = (iso?: string | null) => (iso ? clock.format(new Date(iso)) : '');
const pointOf = (p: Parada) => (p.lat != null && p.lng != null ? { lat: p.lat, lng: p.lng } : null);

/**
 * The chofer's paradas on a day (GET /chofer/ruta) with "En camino" and "Llegué" for today's ruta. The API decides
 * the order, which parada is next and whether an arrival is close enough; the app only asks and shows.
 */
export function RutaDelDia({ onShown }: { onShown?: (ruta: Ruta | null) => void } = {}) {
  const { colors } = useTheme();
  const [date, setDate] = useState<string | null>(null);
  // As the API (or the saved copy) sent it; the screen shows it with the actions that still wait for signal.
  const [base, setBase] = useState<Ruta | null>(null);
  const [today, setToday] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [closing, setClosing] = useState<Parada | null>(null);
  // Without signal: when the ruta on screen was saved.
  const [savedAt, setSavedAt] = useState<string | null>(null);
  const queue = useQueue();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const ruta = useMemo(() => (base ? withPending(base) : null), [base, queue]);

  // The map shows the same paradas, with the actions still waiting for signal.
  useEffect(() => onShown?.(ruta), [ruta, onShown]);

  const load = useCallback(async (fecha: string | null) => {
    setLoading(true);
    setError(null);
    try {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), 15_000);
      const data = await unwrap(api.GET('/chofer/ruta', { params: { query: fecha ? { fecha } : {} }, signal: controller.signal })).finally(() =>
        clearTimeout(timer),
      );
      saveRuta(fecha, data);
      setBase(data);
      setSavedAt(null);
      if (!fecha) setToday(data.date);
    } catch (e) {
      const saved = e instanceof ApiError && e.status === 0 ? savedRuta(fecha) : null;
      if (saved) {
        setBase(saved.ruta);
        setSavedAt(saved.savedAt);
        if (!fecha) setToday(saved.ruta.date);
      } else {
        setError((e as Error).message);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load(date);
  }, [date, load]);

  // Answers of actions sent in the background (signal is back).
  useEffect(
    () =>
      onRuta((r) => {
        setBase((current) => (current && current.date === r.date ? r : current));
        setSavedAt(null);
      }),
    [],
  );

  /** Sends an action, or keeps it for when the signal is back and shows it as done. */
  async function run(action: Action) {
    const result = await submit(action);
    // Queued: the screen already shows it through the pending actions.
    if (!result.queued && result.ruta) {
      saveRuta(null, result.ruta);
      setBase(result.ruta);
      setSavedAt(null);
    }
    return result;
  }

  async function enCamino(p: Parada) {
    setBusyId(p.id);
    setError(null);
    try {
      const position = await currentPosition();
      await run({
        kind: 'en-camino',
        paradaId: p.id,
        body: { lat: position?.lat ?? null, lng: position?.lng ?? null, localId: newLocalId(), at: new Date().toISOString() },
      });
      openNavigation(p.invitadoName ?? KIND[p.kind], pointOf(p), p.address ?? null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusyId(null);
    }
  }

  async function llegue(p: Parada, confirmOutsideRadius = false) {
    setBusyId(p.id);
    setError(null);
    try {
      const position = await currentPosition();
      await run({
        kind: 'llegue',
        paradaId: p.id,
        body: { lat: position?.lat ?? null, lng: position?.lng ?? null, confirmOutsideRadius, localId: newLocalId(), at: new Date().toISOString() },
      });
    } catch (e) {
      // Farther than the tolerance: the API asks to confirm; the office will see it marked.
      if (e instanceof ApiError && e.status === 409) {
        RNAlert.alert('¿Registrar tu llegada?', e.message, [
          { text: 'Cancelar', style: 'cancel' },
          { text: 'Registrar', onPress: () => void llegue(p, true) },
        ]);
      } else {
        setError((e as Error).message);
      }
    } finally {
      setBusyId(null);
    }
  }

  const current = ruta?.date ?? date;
  const pending = ruta?.paradas.filter((p) => p.state !== 'Realizada' && p.state !== 'NoRealizada').length ?? 0;
  const enRoute = ruta?.isToday ? ruta.paradas.find((p) => p.state === 'EnCamino') : undefined;

  return (
    <Card>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm, flexWrap: 'wrap' }}>
        <Ionicons name="list" size={20} color={colors.brand} />
        <View style={{ flex: 1, minWidth: 160 }}>
          <Title size="lg">{ruta?.ruta ? `Ruta ${ruta.ruta.code}` : 'Paradas del día'}</Title>
          {ruta && (
            <Text style={{ color: colors.textMuted, fontSize: 13, textTransform: 'capitalize' }}>
              {ruta.label}
              {ruta.paradas.length > 0 && ` · ${ruta.paradas.length} paradas, ${pending} pendientes`}
            </Text>
          )}
        </View>
        <DayButton icon="chevron-back" label="Día anterior" disabled={!current || loading} onPress={() => current && setDate(addDays(current, -1))} />
        <DayButton icon="today-outline" label="Hoy" disabled={loading || current === today} onPress={() => setDate(today)} />
        <DayButton icon="chevron-forward" label="Día siguiente" disabled={!current || loading} onPress={() => current && setDate(addDays(current, 1))} />
        <DayButton icon="refresh" label="Actualizar" disabled={loading} onPress={() => void load(date)} />
      </View>

      {loading && !ruta && <ActivityIndicator color={colors.brand} style={{ padding: space.lg }} />}
      {error && <Alert>{error}</Alert>}
      {savedAt && (
        <Alert tone="warning">
          Sin señal: ves la ruta guardada a las {at(savedAt)}. Puedes seguir trabajando; lo que hagas se envía solo al volver la señal.
        </Alert>
      )}
      {queue.pending > 0 && (
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: space.sm, flexWrap: 'wrap' }}>
          <View style={{ flex: 1, minWidth: 180 }}>
            <Alert tone="info">
              {queue.pending === 1 ? '1 acción espera señal' : `${queue.pending} acciones esperan señal`}. Se envían solas en orden.
            </Alert>
          </View>
          <Button title="Enviar ahora" variant="secondary" onPress={() => void flush().then(() => load(date))} />
        </View>
      )}
      {queue.failed.map((f) => (
        <View key={f.id} style={{ gap: space.xs }}>
          <Alert>
            No se guardó "{ACTION_NAME[f.kind]}" de la parada {ruta?.paradas.find((p) => p.id === f.paradaId)?.order ?? ''}: {f.error}
          </Alert>
          <Button
            title="Entendido"
            variant="ghost"
            onPress={() => {
              discard(f.id);
              void load(date);
            }}
          />
        </View>
      ))}
      {ruta?.notice && <Alert tone="info">{ruta.notice}</Alert>}
      {ruta?.ruta && ruta.paradas.length > 0 && !ruta.isToday && <Body muted>Solo consulta: "En camino" y "Llegué" son para la ruta de hoy.</Body>}
      {enRoute && <EnRouteBanner parada={enRoute} tolerance={ruta!.toleranceMeters} busy={busyId === enRoute.id} onArrive={() => void llegue(enRoute)} />}

      <View style={{ gap: space.sm, opacity: loading ? 0.5 : 1 }}>
        {ruta?.paradas.map((p) => (
          <ParadaCard
            key={p.id}
            parada={p}
            color={ruta.ruta?.color}
            isNext={ruta.isToday && p.id === ruta.nextParadaId}
            canStart={ruta.isToday && !enRoute && p.state === 'Pendiente' && (!ruta.orderRequired || p.id === ruta.nextParadaId)}
            busy={busyId === p.id}
            onEnCamino={() => void enCamino(p)}
            onLlegue={() => void llegue(p)}
            canClose={!!ruta.isToday && (p.state === 'EnCamino' || p.state === 'EnSitio')}
            onClose={() => setClosing(p)}
          />
        ))}
      </View>
      {closing && ruta && (
        <CerrarVisita
          parada={closing}
          photoRequired={ruta.photoRequired}
          onClose={() => setClosing(null)}
          onDone={(next, queued) => {
            setClosing(null);
            if (next) {
              saveRuta(null, next);
              setBase(next);
            }
            RNAlert.alert(
              'Visita cerrada',
              queued ? 'Sin señal: quedó guardada en el teléfono y se envía sola al volver la señal.' : (next?.message ?? 'Visita cerrada.'),
            );
          }}
        />
      )}
    </Card>
  );
}

/** While en camino: how far the parada is, and "Llegué" highlighted once inside the tolerance. */
function EnRouteBanner({ parada: p, tolerance, busy, onArrive }: { parada: Parada; tolerance: number; busy: boolean; onArrive: () => void }) {
  const { colors } = useTheme();
  const distance = useDistanceTo(pointOf(p));
  const near = distance != null && distance <= tolerance;
  return (
    <View
      style={{
        gap: space.sm,
        padding: space.md,
        borderRadius: radius.md,
        backgroundColor: near ? colors.brand : colors.brandSoft,
      }}
    >
      <Text style={{ color: near ? colors.onBrand : colors.brand, fontWeight: '700', fontSize: 16 }}>
        {near ? `Ya estás a ${distance} m de ${p.invitadoName}` : `En camino a la parada ${p.order}: ${p.invitadoName}`}
      </Text>
      {!near && (
        <Text style={{ color: colors.brand }}>
          {distance != null ? `A ${distance >= 1000 ? `${(distance / 1000).toFixed(1)} km` : `${distance} m`}` : 'Sin ubicación del teléfono'}
          {p.estimatedArrival && ` · hora estimada ${p.estimatedArrival.slice(0, 5)}`}
        </Text>
      )}
      <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
        <Button title="Llegué" icon={<Ionicons name="flag" size={18} color={near ? colors.brand : colors.onBrand} />} busy={busy} onPress={onArrive}
          variant={near ? 'secondary' : 'primary'} />
        <Button title="Navegar" variant="ghost" onPress={() => openNavigation(p.invitadoName ?? KIND[p.kind], pointOf(p), p.address ?? null)} />
      </View>
    </View>
  );
}

function ParadaCard({
  parada: p,
  color,
  isNext,
  canStart,
  busy,
  onEnCamino,
  onLlegue,
  canClose,
  onClose,
}: {
  parada: Parada;
  color?: string;
  isNext: boolean;
  canStart: boolean;
  busy: boolean;
  onEnCamino: () => void;
  onLlegue: () => void;
  canClose: boolean;
  onClose: () => void;
}) {
  const { colors } = useTheme();
  const when = p.turno === 'HoraEspecifica' && p.specificTime ? p.specificTime.slice(0, 5) : TURNO[p.turno];
  const done = p.state === 'Realizada' || p.state === 'NoRealizada';

  return (
    <View
      style={{
        gap: space.sm,
        padding: space.md,
        borderRadius: radius.md,
        borderWidth: isNext ? 2 : 1,
        borderColor: isNext ? colors.brand : colors.border,
        borderLeftWidth: 5,
        borderLeftColor: color ?? colors.brand,
        backgroundColor: colors.surface,
        opacity: done ? 0.55 : 1,
      }}
    >
      <View style={{ flexDirection: 'row', gap: space.md, alignItems: 'flex-start' }}>
        <View style={{ alignItems: 'center', gap: 2 }}>
          <View style={{ width: 36, height: 36, borderRadius: 18, backgroundColor: colors.brandSoft, alignItems: 'center', justifyContent: 'center' }}>
            <Text style={{ color: colors.brand, fontWeight: '700' }}>{p.order}</Text>
          </View>
          {p.estimatedArrival && <Text style={{ color: colors.textMuted, fontSize: 12, fontWeight: '600' }}>{p.estimatedArrival.slice(0, 5)}</Text>}
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Text style={{ color: colors.text, fontWeight: '700', fontSize: 16 }}>{p.invitadoName ?? KIND[p.kind]}</Text>
          <Text style={{ color: colors.textMuted, fontSize: 13 }}>
            {p.category && p.category !== 'ClienteFinal' ? `${CATEGORY[p.category]} · ` : ''}
            {KIND[p.kind]} · {when}
            {p.zonaCode ? ` · ${p.zonaCode}` : ''}
            {p.priority ? ' · PRIORIDAD' : ''}
          </Text>
          <StateLine parada={p} />
          {p.viaje && <Text style={{ color: colors.brand, fontSize: 13, fontWeight: '600' }}>{p.viaje}</Text>}
          {p.activity && <Text style={{ color: colors.text, fontSize: 14, fontWeight: '600' }}>{p.activity}</Text>}
          {p.before && <Text style={{ color: colors.warning, fontSize: 13, fontWeight: '600' }}>Antes de las {p.before.slice(0, 5)}</Text>}
          {p.address && <Text style={{ color: colors.text, fontSize: 14 }}>{p.address}</Text>}
          {p.references && <Text style={{ color: colors.textMuted, fontSize: 13 }}>Ref.: {p.references}</Text>}
          {(p.lineasServicio.length > 0 || p.garmentsApprox != null) && (
            <Text style={{ color: colors.textMuted, fontSize: 13 }}>
              {p.lineasServicio.join(', ')}
              {p.garmentsApprox != null && `${p.lineasServicio.length ? ' · ' : ''}${p.garmentsApprox} prendas aprox.`}
            </Text>
          )}
          {p.notes && <Text style={{ color: colors.warning, fontSize: 13 }}>Nota: {p.notes}</Text>}
        </View>
      </View>
      {canStart && (
        <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
          <Button title="En camino" icon={<Ionicons name="car" size={18} color={colors.onBrand} />} busy={busy} onPress={onEnCamino} />
          <Button title="Llegué" variant="secondary" disabled={busy} onPress={onLlegue} />
          <Button title="Cerrar visita" variant="ghost" disabled={busy} onPress={onClose} />
        </View>
      )}
      {canClose && (
        <Button title="Cerrar visita" icon={<Ionicons name="checkmark-done" size={18} color={colors.onBrand} />} onPress={onClose} />
      )}
      <View style={{ flexDirection: 'row', gap: space.sm, flexWrap: 'wrap' }}>
        {p.phone && <Action icon="call" label="Llamar" onPress={() => void Linking.openURL(`tel:${p.phone}`)} />}
        {p.phone && <Action icon="logo-whatsapp" label="WhatsApp" onPress={() => void Linking.openURL(`https://wa.me/52${p.phone}`)} />}
        {(pointOf(p) || p.address) && (
          <Action icon="navigate" label="Cómo llegar" onPress={() => openNavigation(p.invitadoName ?? KIND[p.kind], pointOf(p), p.address ?? null)} />
        )}
      </View>
    </View>
  );
}

function StateLine({ parada: p }: { parada: Parada }) {
  const { colors } = useTheme();
  if (p.state === 'EnCamino') {
    return <Text style={{ color: colors.brand, fontSize: 13, fontWeight: '700' }}>En camino desde las {at(p.enCaminoAt)}</Text>;
  }
  if (p.state === 'EnSitio') {
    return (
      <Text style={{ color: p.arrivedOutsideRadius ? colors.warning : colors.success, fontSize: 13, fontWeight: '700' }}>
        Llegaste a las {at(p.arrivedAt)}
        {p.arrivedOutsideRadius && p.arrivedDistanceMeters != null && ` · a ${p.arrivedDistanceMeters} m (fuera de radio)`}
        {' · falta cerrar la visita'}
      </Text>
    );
  }
  if (p.state === 'Realizada' || p.state === 'NoRealizada') {
    const done = p.state === 'Realizada';
    return (
      <Text style={{ color: done ? colors.success : colors.danger, fontSize: 13, fontWeight: '700' }}>
        {done ? 'Realizada' : 'No realizada'}
        {p.closedAt && ` a las ${at(p.closedAt)}`}
        {p.photos > 0 && ` · ${p.photos} foto${p.photos === 1 ? '' : 's'}`}
        {p.signed && ' · firmada'}
      </Text>
    );
  }
  return null;
}

function Action({ icon, label, onPress }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={label}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: 6,
        minHeight: touch - 4,
        paddingHorizontal: space.md,
        borderRadius: radius.md,
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: pressed ? colors.surface2 : colors.surface,
      })}
    >
      <Ionicons name={icon} size={18} color={colors.brand} />
      <Text style={{ color: colors.text, fontWeight: '600' }}>{label}</Text>
    </Pressable>
  );
}

function DayButton({ icon, label, onPress, disabled }: { icon: keyof typeof Ionicons.glyphMap; label: string; onPress: () => void; disabled?: boolean }) {
  const { colors } = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      accessibilityRole="button"
      accessibilityLabel={label}
      hitSlop={4}
      style={({ pressed }) => ({
        width: touch - 4,
        height: touch - 4,
        borderRadius: radius.md,
        alignItems: 'center',
        justifyContent: 'center',
        borderWidth: 1,
        borderColor: colors.border,
        backgroundColor: pressed ? colors.surface2 : colors.surface,
        opacity: disabled ? 0.4 : 1,
      })}
    >
      <Ionicons name={icon} size={20} color={colors.text} />
    </Pressable>
  );
}

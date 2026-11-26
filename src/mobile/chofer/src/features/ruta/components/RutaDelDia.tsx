import { Alert, ApiError, Body, Button, Card, space, Title, useTheme } from '@cleancom/mobile-shared';
import { choferApi } from '../../../common/api/choferApi';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Alert as RNAlert, Text, View } from 'react-native';
import Ionicons from '@expo/vector-icons/Ionicons';
import { addDays, at, KIND, type Parada, pointOf, type Ruta } from '../types/ruta';
import {
  ACTION_NAME,
  discard,
  flush,
  newLocalId,
  onRuta,
  savedRuta,
  saveRuta,
  submit,
  useQueue,
  withPending,
  type Action,
} from '../../../offline/cola';
import { ParadaCard } from './ParadaCard';
import { currentPosition, openNavigation } from '../../../common/ubicacion';
import { DayButton } from './DayButton';
import { EnRouteBanner } from './EnRouteBanner';
import { CerrarVisita } from '../../visita';

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

  // Asks the API (or, without signal, the saved copy) for a day. Sets state only once the answer is back.
  const fetchRuta = useCallback((fecha: string | null) => {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 15_000);
    return choferApi
      .ruta(fecha ? { fecha } : {}, controller.signal)
      .finally(() => clearTimeout(timer))
      .then(
        (data) => {
          saveRuta(fecha, data);
          setBase(data);
          setSavedAt(null);
          if (!fecha) setToday(data.date);
        },
        (e) => {
          const saved = e instanceof ApiError && e.status === 0 ? savedRuta(fecha) : null;
          if (saved) {
            setBase(saved.ruta);
            setSavedAt(saved.savedAt);
            if (!fecha) setToday(saved.ruta.date);
          } else {
            setError((e as Error).message);
          }
        },
      )
      .finally(() => setLoading(false));
  }, []);

  /** Reloads a day from a button: shows it as loading right away. */
  const load = useCallback(
    (fecha: string | null) => {
      setLoading(true);
      setError(null);
      return fetchRuta(fecha);
    },
    [fetchRuta],
  );

  function showDay(fecha: string | null) {
    setDate(fecha);
    void load(fecha);
  }

  // First load: the state already starts as loading.
  useEffect(() => {
    void fetchRuta(null);
  }, [fetchRuta]);

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
  // The shift is required: today's paradas are worked only between "iniciar turno" and "cerrar turno".
  const shiftOpen = !!ruta?.shift?.startedAt && !ruta.shift.endedAt;

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
        <DayButton icon="chevron-back" label="Día anterior" disabled={!current || loading} onPress={() => current && showDay(addDays(current, -1))} />
        <DayButton icon="today-outline" label="Hoy" disabled={loading || current === today} onPress={() => showDay(today)} />
        <DayButton
          icon="chevron-forward"
          label="Día siguiente"
          disabled={!current || loading}
          onPress={() => current && showDay(addDays(current, 1))}
        />
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
            No se guardó “{ACTION_NAME[f.kind]}”
            {f.paradaId > 0 && ` de la parada ${ruta?.paradas.find((p) => p.id === f.paradaId)?.order ?? ''}`}: {f.error}
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
      {ruta?.ruta && ruta.paradas.length > 0 && !ruta.isToday && <Body muted>Solo consulta: “En camino” y “Llegué” son para la ruta de hoy.</Body>}
      {ruta?.isToday && ruta.paradas.length > 0 && !ruta.shift?.startedAt && (
        <Alert tone="info">Inicia tu turno para salir a tus paradas.</Alert>
      )}
      {enRoute && shiftOpen && (
        <EnRouteBanner parada={enRoute} tolerance={ruta!.toleranceMeters} busy={busyId === enRoute.id} onArrive={() => void llegue(enRoute)} />
      )}

      <View style={{ gap: space.sm, opacity: loading ? 0.5 : 1 }}>
        {ruta?.paradas.map((p) => (
          <ParadaCard
            key={p.id}
            parada={p}
            color={ruta.ruta?.color}
            isNext={ruta.isToday && p.id === ruta.nextParadaId}
            canStart={ruta.isToday && shiftOpen && !enRoute && p.state === 'Pendiente' && (!ruta.orderRequired || p.id === ruta.nextParadaId)}
            busy={busyId === p.id}
            onEnCamino={() => void enCamino(p)}
            onLlegue={() => void llegue(p)}
            canClose={!!ruta.isToday && shiftOpen && (p.state === 'EnCamino' || p.state === 'EnSitio')}
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

import { useEffect, useState } from 'react';
import { AppState, Platform } from 'react-native';
import * as Location from 'expo-location';
import * as TaskManager from 'expo-task-manager';
import * as SQLite from 'expo-sqlite';
import { ApiError, api, getSession, loadSession, unwrap, type Schemas } from '@cleancom/mobile-shared';

/**
 * The chofer's GPS during the shift (docs/03-reglas-de-negocio.md, GPS del chofer): the phone reads its position every
 * 30 to 60 seconds, also with the app in the background, and sends it to POST /chofer/ubicacion. Without signal the
 * readings wait in SQLite and go out together when the connection is back. The API decides what it keeps.
 */

const TASK = 'cleancom-ubicacion';
/** The API takes up to 2000 at once; smaller batches keep each request light. */
const BATCH = 300;
/** A day without signal at one reading every 30 s; older readings are dropped first. */
const MAX_STORED = 3000;

type Ruta = Schemas['ChoferRutaDto'];
type Row = { id: number; lat: number; lng: number; at: string; accuracy: number | null };

const db = SQLite.openDatabaseSync('cleancom-chofer.db');
db.execSync(`
  CREATE TABLE IF NOT EXISTS locations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    lat REAL NOT NULL,
    lng REAL NOT NULL,
    at TEXT NOT NULL,
    accuracy REAL
  );
`);

// Defined when the module loads (imported from the root layout), so the OS can run it with the app closed.
TaskManager.defineTask<{ locations: Location.LocationObject[] }>(TASK, async ({ data, error }) => {
  if (error || !data) return;
  const session = getSession() ?? (await loadSession());
  if (!session) return;
  store(session.user.id, data.locations);
  await sendStored();
});

function store(userId: number, locations: Location.LocationObject[]) {
  for (const l of locations) {
    db.runSync(
      'INSERT INTO locations (user_id, lat, lng, at, accuracy) VALUES (?, ?, ?, ?, ?)',
      userId,
      l.coords.latitude,
      l.coords.longitude,
      new Date(l.timestamp).toISOString(),
      l.coords.accuracy ?? null,
    );
  }
  db.runSync('DELETE FROM locations WHERE id NOT IN (SELECT id FROM locations ORDER BY id DESC LIMIT ?)', MAX_STORED);
}

let sending: Promise<void> | null = null;

/** Sends the stored readings of the signed-in chofer in batches; what fails for lack of signal stays for later. */
export function sendStored(): Promise<void> {
  sending ??= (async () => {
    try {
      const userId = (getSession() ?? (await loadSession()))?.user.id;
      if (!userId) return;
      for (;;) {
        const rows = db.getAllSync<Row>('SELECT id, lat, lng, at, accuracy FROM locations WHERE user_id = ? ORDER BY id LIMIT ?', userId, BATCH);
        if (rows.length === 0) return;
        try {
          await unwrap(
            api.POST('/chofer/ubicacion', {
              body: { readings: rows.map((r) => ({ lat: r.lat, lng: r.lng, at: r.at, accuracyMeters: r.accuracy })) },
            }),
          );
        } catch (e) {
          // No signal (0), session to renew (401) or the server down: try again later. Anything else would fail
          // forever, so that batch is dropped.
          if (!(e instanceof ApiError) || e.status === 0 || e.status === 401 || e.status >= 500) return;
        }
        db.runSync(`DELETE FROM locations WHERE id IN (${rows.map((r) => r.id).join(',')})`);
      }
    } finally {
      sending = null;
    }
  })();
  return sending;
}

/** Whether the shift should be followed: today's ruta with paradas still to visit. */
export function shouldTrack(ruta: Ruta): boolean {
  return ruta.isToday && ruta.ruta != null && ruta.paradas.some((p) => p.state !== 'Realizada' && p.state !== 'NoRealizada');
}

export type TrackingState = 'off' | 'on' | 'onlyOpen' | 'denied';

/**
 * Starts following the chofer. Asks for "always" so it keeps working with the app in the background; with only
 * "while in use" it works while the app is open (Android keeps it going with its notification).
 */
export async function startTracking(): Promise<TrackingState> {
  const foreground = await Location.requestForegroundPermissionsAsync();
  if (foreground.status !== 'granted') return 'denied';
  const background = await Location.requestBackgroundPermissionsAsync().catch(() => ({ status: 'denied' as const }));
  if (!(await Location.hasStartedLocationUpdatesAsync(TASK).catch(() => false))) {
    await Location.startLocationUpdatesAsync(TASK, {
      accuracy: Location.Accuracy.Balanced,
      timeInterval: 45_000,
      distanceInterval: 50,
      deferredUpdatesInterval: 45_000,
      pausesUpdatesAutomatically: false,
      activityType: Location.ActivityType.AutomotiveNavigation,
      showsBackgroundLocationIndicator: true,
      foregroundService: {
        notificationTitle: 'CleanCom Chofer',
        notificationBody: 'Compartiendo tu ubicación con oficina durante el turno.',
        notificationColor: '#14276D',
      },
    });
  }
  return background.status === 'granted' ? 'on' : 'onlyOpen';
}

/** Stops following and sends what is left. */
export async function stopTracking() {
  if (await Location.hasStartedLocationUpdatesAsync(TASK).catch(() => false)) {
    await Location.stopLocationUpdatesAsync(TASK);
  }
  await sendStored();
}

/**
 * Follows the shift while today's ruta has paradas to visit, and stops when they are all closed. A ruta of another
 * day on screen does not change anything. Also sends what is stored when the app comes back.
 */
export function useShiftTracking(ruta: Ruta | null): TrackingState {
  const [state, setState] = useState<TrackingState>('off');
  const track = ruta?.isToday ? shouldTrack(ruta) : null;

  useEffect(() => {
    if (track === null || Platform.OS === 'web') return;
    let cancelled = false;
    void (track ? startTracking() : stopTracking().then(() => 'off' as const))
      .then((s) => !cancelled && setState(s))
      .catch(() => !cancelled && setState('denied'));
    return () => {
      cancelled = true;
    };
  }, [track]);

  useEffect(() => {
    const subscription = AppState.addEventListener('change', (s) => s === 'active' && void sendStored());
    return () => subscription.remove();
  }, []);

  return state;
}

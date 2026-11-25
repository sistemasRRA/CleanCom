import { ApiError, getSession, type Schemas } from '@cleancom/mobile-shared';
import { choferApi } from '../common/api/choferApi';
import * as SQLite from 'expo-sqlite';
import { useEffect, useState } from 'react';
import { AppState } from 'react-native';
import { Directory, File, Paths } from 'expo-file-system';

/**
 * Sin señal (docs/pantallas/apps/chofer.md, Requisitos técnicos): every action of the chofer is saved in SQLite with
 * a local id and sent in order; when there is no signal it waits and goes out by itself when the connection is back.
 * The API does not save a resend twice. The last ruta is kept to keep working without signal.
 */

type Ruta = Schemas['ChoferRutaDto'];

type Parada = Schemas['ChoferParadaDto'];

export type Action =
  | { kind: 'en-camino'; paradaId: number; body: Schemas['ChoferLocationRequest'] }
  | { kind: 'llegue'; paradaId: number; body: Schemas['ArriveRequest'] }
  | { kind: 'foto'; paradaId: number; body: { localId: string; at: string; lat: number | null; lng: number | null }; fileUri: string }
  | { kind: 'firma'; paradaId: number; body: Schemas['SignatureRequest'] }
  | { kind: 'cerrar'; paradaId: number; body: Schemas['CloseVisitRequest'] }
  | { kind: 'no-realizada'; paradaId: number; body: Schemas['NotDoneRequest'] };

type Row = { id: number; user_id: number; kind: Action['kind']; parada_id: number; payload: string; file_uri: string | null; error: string | null };

export type Failed = { id: number; kind: Action['kind']; paradaId: number; error: string };

export type QueueState = { pending: number; failed: Failed[] };

export const ACTION_NAME: Record<Action['kind'], string> = {
  'en-camino': 'En camino',
  llegue: 'Llegué',
  foto: 'Foto',
  firma: 'Firma',
  cerrar: 'Cerrar visita',
  'no-realizada': 'No realizada',
};

/** An action that waits for signal ("queued") or that the API already answered. */
export type Submitted = { queued: true } | { queued: false; ruta: Ruta | null };

export const newLocalId = () => `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;

const db = SQLite.openDatabaseSync('cleancom-chofer.db');

db.execSync(`
  CREATE TABLE IF NOT EXISTS queue (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    kind TEXT NOT NULL,
    parada_id INTEGER NOT NULL,
    payload TEXT NOT NULL,
    file_uri TEXT,
    created_at TEXT NOT NULL,
    error TEXT,
    error_status INTEGER
  );
  CREATE TABLE IF NOT EXISTS cache (key TEXT PRIMARY KEY, json TEXT NOT NULL, saved_at TEXT NOT NULL);
`);

const userId = () => getSession()?.user.id ?? 0;

// ---- Listeners ----

const queueListeners = new Set<(s: QueueState) => void>();

const rutaListeners = new Set<(r: Ruta) => void>();

function queueState(): QueueState {
  const rows = db.getAllSync<Row>('SELECT * FROM queue WHERE user_id = ? ORDER BY id', userId());
  return {
    pending: rows.filter((r) => !r.error).length,
    failed: rows.filter((r) => r.error).map((r) => ({ id: r.id, kind: r.kind, paradaId: r.parada_id, error: r.error! })),
  };
}

function notify() {
  const s = queueState();
  queueListeners.forEach((l) => l(s));
}

/** Pending and failed actions of the signed-in chofer; sends them when the app comes back and every 20 s. */
export function useQueue() {
  const [state, setState] = useState<QueueState>(() => queueState());
  useEffect(() => {
    queueListeners.add(setState);
    setState(queueState());
    const appState = AppState.addEventListener('change', (s) => s === 'active' && void flush());
    const timer = setInterval(() => queueState().pending > 0 && void flush(), 20_000);
    void flush();
    return () => {
      queueListeners.delete(setState);
      appState.remove();
      clearInterval(timer);
    };
  }, []);
  return state;
}

/** Answers that arrive while sending in the background update the screen. */
export function onRuta(listener: (r: Ruta) => void) {
  rutaListeners.add(listener);
  return () => {
    rutaListeners.delete(listener);
  };
}

// ---- Saved ruta ----

const cacheKey = (fecha: string | null) => `${userId()}:${fecha ?? 'hoy'}`;

export function saveRuta(fecha: string | null, ruta: Ruta) {
  const now = new Date().toISOString();
  for (const key of new Set([cacheKey(fecha), cacheKey(ruta.date)])) {
    db.runSync('INSERT OR REPLACE INTO cache (key, json, saved_at) VALUES (?, ?, ?)', key, JSON.stringify(ruta), now);
  }
}

export function savedRuta(fecha: string | null): { ruta: Ruta; savedAt: string } | null {
  const row = db.getFirstSync<{ json: string; saved_at: string }>('SELECT json, saved_at FROM cache WHERE key = ?', cacheKey(fecha));
  return row ? { ruta: JSON.parse(row.json) as Ruta, savedAt: row.saved_at } : null;
}

// ---- Sending ----

/**
 * Saves the action and tries to send it with everything before it. Online, it waits for the answer: an error of the
 * API (rules, 409 to confirm) is thrown so the screen shows it, and the action is dropped. Without signal it stays
 * queued and the screen shows it as done.
 */
export async function submit(action: Action): Promise<Submitted> {
  let fileUri: string | null = null;
  if (action.kind === 'foto') {
    // The camera's copy may be cleaned by the system; the pending photo is kept in the app's documents.
    const dir = new Directory(Paths.document, 'pendientes');
    if (!dir.exists) dir.create({ intermediates: true, idempotent: true });
    const copy = new File(dir, `${action.body.localId}.jpg`);
    await new File(action.fileUri).copy(copy);
    fileUri = copy.uri;
  }
  const result = db.runSync(
    'INSERT INTO queue (user_id, kind, parada_id, payload, file_uri, created_at) VALUES (?, ?, ?, ?, ?, ?)',
    userId(),
    action.kind,
    action.paradaId,
    JSON.stringify(action.body),
    fileUri,
    new Date().toISOString(),
  );
  const id = Number(result.lastInsertRowId);
  notify();
  await flush();
  // A send already finishing may have missed this action: one more pass.
  if (db.getFirstSync('SELECT id FROM queue WHERE id = ? AND error IS NULL', id)) await flush();

  const row = db.getFirstSync<Row & { error_status: number | null }>('SELECT * FROM queue WHERE id = ?', id);
  if (!row) {
    const ruta = answers.get(id) ?? null;
    answers.delete(id);
    return { queued: false, ruta };
  }
  if (row.error) {
    // The chofer is looking at the screen: the error is shown there instead of in the list of failed actions.
    remove(row);
    throw new ApiError(row.error_status ?? 400, row.error);
  }
  return { queued: true };
}

/** Discards a failed action (the chofer saw the error). */
export function discard(id: number) {
  const row = db.getFirstSync<Row>('SELECT * FROM queue WHERE id = ?', id);
  if (row) remove(row);
}

const answers = new Map<number, Ruta | null>();

let running: Promise<void> | null = null;

/** Sends the queue in order. Stops at the first network failure; an error of the API marks that action and goes on. */
export function flush(): Promise<void> {
  running ??= (async () => {
    try {
      for (;;) {
        const row = db.getFirstSync<Row>('SELECT * FROM queue WHERE user_id = ? AND error IS NULL ORDER BY id LIMIT 1', userId());
        if (!row) break;
        try {
          const ruta = await send(row);
          answers.set(row.id, ruta);
          remove(row);
          if (ruta) {
            saveRuta(null, ruta);
            rutaListeners.forEach((l) => l(ruta));
          }
        } catch (e) {
          const status = e instanceof ApiError ? e.status : 0;
          // No signal, server down or session to renew: try again later, in the same order.
          if (status === 0 || status >= 500 || status === 401) break;
          db.runSync('UPDATE queue SET error = ?, error_status = ? WHERE id = ?', (e as Error).message, status, row.id);
        }
        notify();
      }
    } finally {
      running = null;
      notify();
    }
  })();
  return running;
}

function remove(row: Pick<Row, 'id' | 'file_uri'>) {
  db.runSync('DELETE FROM queue WHERE id = ?', row.id);
  if (row.file_uri) {
    try {
      new File(row.file_uri).delete();
    } catch {
      // Already gone.
    }
  }
  notify();
}

/** Bad signal can leave a request hanging; it is given up and retried later. */
function timeout(ms: number) {
  const controller = new AbortController();
  setTimeout(() => controller.abort(), ms);
  return controller.signal;
}

async function send(row: Row): Promise<Ruta | null> {
  const body = JSON.parse(row.payload);
  const signal = timeout(row.kind === 'foto' ? 60_000 : 15_000);
  switch (row.kind) {
    case 'en-camino':
      return choferApi.enCamino(row.parada_id, body, signal);
    case 'llegue':
      return choferApi.llegue(row.parada_id, body, signal);
    case 'firma':
      await choferApi.firma(body, signal);
      return null;
    case 'cerrar':
      return choferApi.cerrar(row.parada_id, body, signal);
    case 'no-realizada':
      return choferApi.noRealizada(row.parada_id, body, signal);
    case 'foto': {
      const form = new FormData();
      form.append('ParadaId', String(row.parada_id));
      form.append('LocalId', body.localId);
      form.append('TakenAt', body.at);
      if (body.lat != null && body.lng != null) {
        form.append('Lat', String(body.lat));
        form.append('Lng', String(body.lng));
      }
      // React Native's FormData takes the file as { uri, name, type }.
      form.append('File', { uri: row.file_uri, name: 'foto.jpg', type: 'image/jpeg' } as unknown as Blob);
      await choferApi.foto(form, signal);
      return null;
    }
  }
}

// ---- What the screen shows while actions wait ----

/** The ruta with the pending actions of this chofer already applied, as they will look once sent. */
export function withPending(ruta: Ruta): Ruta {
  const rows = db.getAllSync<Row>('SELECT * FROM queue WHERE user_id = ? AND error IS NULL ORDER BY id', userId());
  return rows.reduce((r, row) => apply(r, row.kind, row.parada_id, JSON.parse(row.payload)), ruta);
}

/** Display only: the API decides; this mirrors what it will answer so the chofer can keep working without signal. */
export function apply(ruta: Ruta, kind: Action['kind'], paradaId: number, body: { at?: string | null }): Ruta {
  if (!ruta.paradas.some((p) => p.id === paradaId)) return ruta;
  const at = body.at ?? new Date().toISOString();
  const paradas = ruta.paradas.map((p): Parada => {
    if (p.id !== paradaId) return p;
    switch (kind) {
      case 'en-camino':
        return { ...p, state: 'EnCamino', enCaminoAt: at };
      case 'llegue':
        return { ...p, state: 'EnSitio', arrivedAt: at, enCaminoAt: p.enCaminoAt ?? at };
      case 'foto':
        return { ...p, photos: p.photos + 1 };
      case 'firma':
        return { ...p, signed: true };
      case 'cerrar':
        return { ...p, state: 'Realizada', closedAt: at };
      case 'no-realizada':
        return { ...p, state: 'NoRealizada', closedAt: at };
    }
  });
  const next = ruta.isToday ? (paradas.find((p) => p.state === 'Pendiente')?.id ?? null) : null;
  return { ...ruta, paradas, nextParadaId: next, message: null };
}

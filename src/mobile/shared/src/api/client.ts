import createClient, { type Middleware } from 'openapi-fetch';
import * as SecureStore from 'expo-secure-store';
import { apiUrl } from '../config';
import type { components, paths } from './schema';

/**
 * API client. Types come from schema.d.ts, generated from the API's OpenAPI with `npm run api:types`
 * (same source as the office web). Never write response types by hand.
 */
export type Schemas = components['schemas'];
export type Session = Schemas['SessionDto'];

// ---- Session stored on the device ----
// Tokens go to SecureStore (Keychain / Keystore). The user (name, role, permissions) is kept apart
// because some iOS versions reject values above ~2 KB.

const TOKENS_KEY = 'cleancom.tokens';
const USER_KEY = 'cleancom.user';

type Tokens = Omit<Session, 'user'>;
type Listener = (session: Session | null) => void;

const listeners = new Set<Listener>();
let currentSession: Session | null = null;

export function getSession() {
  return currentSession;
}

/** Reads the saved session once, when the app starts. */
export async function loadSession(): Promise<Session | null> {
  try {
    const [tokens, user] = await Promise.all([SecureStore.getItemAsync(TOKENS_KEY), SecureStore.getItemAsync(USER_KEY)]);
    currentSession = tokens && user ? { ...(JSON.parse(tokens) as Tokens), user: JSON.parse(user) } : null;
  } catch {
    currentSession = null;
  }
  return currentSession;
}

export async function saveSession(session: Session | null) {
  currentSession = session;
  listeners.forEach((l) => l(session));
  try {
    if (session) {
      const { user, ...tokens } = session;
      await Promise.all([
        SecureStore.setItemAsync(TOKENS_KEY, JSON.stringify(tokens)),
        SecureStore.setItemAsync(USER_KEY, JSON.stringify(user)),
      ]);
    } else {
      await Promise.all([SecureStore.deleteItemAsync(TOKENS_KEY), SecureStore.deleteItemAsync(USER_KEY)]);
    }
  } catch {
    // Without secure storage the session lasts until the app closes.
  }
}

export function onSessionChange(listener: Listener) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

// ---- Token refresh ----

let refreshing: Promise<Session | null> | null = null;

/** Refreshes only once even when several requests get a 401 at the same time. */
function refreshSession(): Promise<Session | null> {
  const refreshToken = currentSession?.refreshToken;
  if (!refreshToken) return Promise.resolve(null);
  refreshing ??= fetch(`${apiUrl()}/auth/renovar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
    .then(async (r) => {
      const next = r.ok ? ((await r.json()) as Session) : null;
      await saveSession(next);
      return next;
    })
    .catch(() => null)
    .finally(() => {
      refreshing = null;
    });
  return refreshing;
}

const pendingCopies = new Map<string, Request>();

const auth: Middleware = {
  onRequest({ request, id }) {
    if (currentSession) request.headers.set('Authorization', `Bearer ${currentSession.accessToken}`);
    pendingCopies.set(id, request.clone());
    return request;
  },
  async onResponse({ request, response, id }) {
    const copy = pendingCopies.get(id);
    pendingCopies.delete(id);
    // Plain string check: React Native's URL class does not implement `pathname`.
    const isAuthCall = /\/auth\//.test(request.url);
    // Returning nothing keeps the response as is. Returning `response` itself fails in Expo:
    // openapi-fetch checks `instanceof Response` and Expo's fetch response is a different class.
    if (response.status !== 401 || !copy || isAuthCall || !currentSession) return undefined;

    const next = await refreshSession();
    if (!next) return undefined;
    copy.headers.set('Authorization', `Bearer ${next.accessToken}`);
    const retry = await fetch(copy);
    // Rebuilt with the global Response so openapi-fetch accepts it.
    const noBody = retry.status === 204 || retry.status === 205 || retry.status === 304;
    return new Response(noBody ? null : await retry.text(), {
      status: retry.status,
      statusText: retry.statusText,
      headers: retry.headers,
    });
  },
  onError({ id }) {
    pendingCopies.delete(id);
  },
};

export const api = createClient<paths>({ baseUrl: apiUrl() });
api.use(auth);

// ---- Errors ----

type Problem = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

/** API error carrying the Spanish message from its ProblemDetails. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fields: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

function toError(error: unknown, response: Response): ApiError {
  const p = (error ?? {}) as Problem;
  if (p.errors && Object.keys(p.errors).length > 0) {
    return new ApiError(response.status, Object.values(p.errors).flat().join(' '), p.errors);
  }
  if (response.status === 401 && !p.detail) return new ApiError(401, 'Tu sesión terminó. Vuelve a iniciar sesión.');
  if (response.status === 403) return new ApiError(403, 'No tienes permiso para esta acción.');
  if (response.status === 404 && !p.detail) return new ApiError(404, 'Esta función todavía no está disponible.');
  return new ApiError(response.status, p.detail ?? p.title ?? `Error ${response.status}.`);
}

/** Turns an openapi-fetch response into data or an ApiError. Also throws when the API cannot be reached. */
export async function unwrap<T>(promise: Promise<{ data?: T; error?: unknown; response: Response }>): Promise<T> {
  let result;
  try {
    result = await promise;
  } catch (e) {
    // Shown in the Metro console so a client bug is not mistaken for a network failure.
    if (__DEV__) console.warn('[api] request failed before a response was read:', e);
    throw new ApiError(0,`No hay conexión con el API (${apiUrl()}). Revisa tu señal o que el servidor esté encendido.`);
  }
  const { data, error, response } = result;
  if (!response.ok) throw toError(error, response);
  return data as T;
}

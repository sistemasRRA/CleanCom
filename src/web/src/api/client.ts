import createClient, { type Middleware } from 'openapi-fetch';
import type { components, paths } from './schema';

/**
 * API client. Types come from schema.d.ts, generated from /openapi/v1.json with `npm run api:types`.
 * Never write response types by hand.
 */
export type Schemas = components['schemas'];
export type Session = Schemas['SessionDto'];

const API_URL = import.meta.env.VITE_API_URL ?? '/api';
const SESSION_KEY = 'cleancom.session';

// ---- Session stored in the browser ----

type Listener = (session: Session | null) => void;
const listeners = new Set<Listener>();

function readSession(): Session | null {
  try {
    const text = localStorage.getItem(SESSION_KEY);
    return text ? (JSON.parse(text) as Session) : null;
  } catch {
    return null;
  }
}

let currentSession = readSession();

export function getSession() {
  return currentSession;
}

export function saveSession(session: Session | null) {
  currentSession = session;
  try {
    if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    else localStorage.removeItem(SESSION_KEY);
  } catch {
    // Without storage the session lasts as long as the tab.
  }
  listeners.forEach((l) => l(session));
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
  refreshing ??= fetch(`${API_URL}/auth/renovar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
    .then(async (r) => {
      const next = r.ok ? ((await r.json()) as Session) : null;
      saveSession(next);
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
  async onResponse({ response, id }) {
    const copy = pendingCopies.get(id);
    pendingCopies.delete(id);
    const isAuthCall = new URL(response.url || 'http://x/').pathname.includes('/auth/');
    if (response.status !== 401 || !copy || isAuthCall || !currentSession) return response;

    const next = await refreshSession();
    if (!next) return response;
    copy.headers.set('Authorization', `Bearer ${next.accessToken}`);
    return fetch(copy);
  },
  onError({ id }) {
    pendingCopies.delete(id);
  },
};

export const api = createClient<paths>({ baseUrl: API_URL });
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
    const messages = Object.values(p.errors).flat();
    return new ApiError(response.status, messages.join(' '), p.errors);
  }
  if (response.status === 401 && !p.detail) return new ApiError(401, 'Tu sesión terminó. Vuelve a iniciar sesión.');
  if (response.status === 403) return new ApiError(403, 'No tienes permiso para esta acción.');
  return new ApiError(response.status, p.detail ?? p.title ?? `Error ${response.status}.`);
}

/**
 * Turns an openapi-fetch response into data or an ApiError, for use inside TanStack Query.
 * Also throws ApiError when the server cannot be reached.
 */
export async function unwrap<T>(
  promise: Promise<{ data?: T; error?: unknown; response: Response }>,
): Promise<T> {
  let result;
  try {
    result = await promise;
  } catch {
    throw new ApiError(0, 'No hay conexión con el API.');
  }
  const { data, error, response } = result;
  if (!response.ok) throw toError(error, response);
  return data as T;
}

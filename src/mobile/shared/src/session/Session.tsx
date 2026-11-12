import { createContext, use, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { AppState } from 'react-native';
import { ApiError, api, getSession, loadSession, onSessionChange, saveSession, unwrap, type Schemas, type Session } from '../api/client';

export type Level = 'view' | 'edit';
export type SessionStatus = 'loading' | 'signedOut' | 'mustChangePassword' | 'signedIn';

type SessionContext = {
  status: SessionStatus;
  user: Schemas['SessionUserDto'] | null;
  login: (userName: string, password: string) => Promise<void>;
  changePassword: (current: string, next: string) => Promise<void>;
  logout: () => Promise<void>;
  /** Only decides what is shown; the API checks the permission again on every request. */
  can: (screen: string, level?: Level) => boolean;
};

const Context = createContext<SessionContext | null>(null);

const allows = (user: Schemas['SessionUserDto'] | undefined, screen: string, level: Level = 'view') => {
  const permissions = user?.permissions ?? [];
  return permissions.includes(`${screen}:edit`) || (level === 'view' && permissions.includes(`${screen}:view`));
};

/**
 * Session of one app. `appScreen` is the permission the app needs (app-chofer, app-invitado): a user
 * whose role lacks it cannot sign in here, and loses the session if the office removes it.
 */
export function SessionProvider({ appScreen, appName, children }: { appScreen: string; appName: string; children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(getSession);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    const off = onSessionChange(setSession);
    void loadSession().then(async (s) => {
      // A session saved before the office removed access does not open the app.
      if (s && !s.user.mustChangePassword && !allows(s.user, appScreen)) await saveSession(null);
      setLoaded(true);
    });
    return off;
  }, [appScreen]);

  // Permissions can change in the office; refresh them each time the app comes back to the front.
  useEffect(() => {
    const sub = AppState.addEventListener('change', async (state) => {
      const current = getSession();
      if (state !== 'active' || !current || current.user.mustChangePassword) return;
      try {
        const user = await unwrap(api.GET('/auth/yo'));
        if (!allows(user, appScreen)) await saveSession(null);
        else if (JSON.stringify(user) !== JSON.stringify(current.user)) await saveSession({ ...current, user });
      } catch {
        // Without connection the saved session stays; the API decides on the next request.
      }
    });
    return () => sub.remove();
  }, [appScreen]);

  const accept = useCallback(
    async (next: Session) => {
      if (!next.user.mustChangePassword && !allows(next.user, appScreen)) {
        await api.POST('/auth/cerrar', { body: { refreshToken: next.refreshToken } }).catch(() => undefined);
        throw new ApiError(403, `Tu usuario (${next.user.roleName}) no tiene acceso a ${appName}. Pide acceso a oficina.`);
      }
      await saveSession(next);
    },
    [appScreen, appName],
  );

  const value = useMemo<SessionContext>(() => {
    const user = session?.user ?? null;
    const status: SessionStatus = !loaded ? 'loading' : !user ? 'signedOut' : user.mustChangePassword ? 'mustChangePassword' : 'signedIn';
    return {
      status,
      user,
      async login(userName, password) {
        await accept(await unwrap(api.POST('/auth/login', { body: { userName, password } })));
      },
      async changePassword(currentPassword, newPassword) {
        await accept(await unwrap(api.POST('/auth/cambiar-contrasena', { body: { currentPassword, newPassword } })));
      },
      async logout() {
        const refreshToken = session?.refreshToken;
        await saveSession(null);
        if (refreshToken) await api.POST('/auth/cerrar', { body: { refreshToken } }).catch(() => undefined);
      },
      can: (screen, level) => allows(user ?? undefined, screen, level),
    };
  }, [session, loaded, accept]);

  return <Context value={value}>{children}</Context>;
}

export function useSession() {
  const context = use(Context);
  if (!context) throw new Error('useSession needs SessionProvider.');
  return context;
}

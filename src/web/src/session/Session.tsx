import { createContext, use, useEffect, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api, getSession, onSessionChange, saveSession, unwrap, type Schemas, type Session } from '../api/client';

export type Level = 'view' | 'edit';

type SessionContext = {
  user: Schemas['SessionUserDto'] | null;
  login: (userName: string, password: string) => Promise<void>;
  changePassword: (current: string, next: string) => Promise<void>;
  logout: () => Promise<void>;
  /** Only decides what is shown; the API checks the permission again on every request. */
  can: (screen: string, level?: Level) => boolean;
};

const Context = createContext<SessionContext | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(getSession);
  const queryClient = useQueryClient();

  useEffect(
    () =>
      onSessionChange((next) => {
        // After signing out, the previous user's data must not stay in memory.
        if (!next) queryClient.clear();
        setSession(next);
      }),
    [queryClient],
  );

  const value: SessionContext = {
    user: session?.user ?? null,

    async login(userName, password) {
      saveSession(await unwrap(api.POST('/auth/login', { body: { userName, password } })));
    },

    async changePassword(currentPassword, newPassword) {
      saveSession(await unwrap(api.POST('/auth/cambiar-contrasena', { body: { currentPassword, newPassword } })));
    },

    async logout() {
      const refreshToken = session?.refreshToken;
      saveSession(null);
      if (refreshToken) await api.POST('/auth/cerrar', { body: { refreshToken } }).catch(() => undefined);
    },

    can(screen, level = 'view') {
      const permissions = session?.user.permissions ?? [];
      return permissions.includes(`${screen}:edit`) || (level === 'view' && permissions.includes(`${screen}:view`));
    },
  };

  return <Context value={value}>{children}</Context>;
}

export function useSession() {
  const context = use(Context);
  if (!context) throw new Error('useSession needs SessionProvider.');
  return context;
}

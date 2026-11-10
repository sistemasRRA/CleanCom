import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ApiError } from './api/client';
import { SessionProvider, useSession } from './session/Session';
import { ToastProvider } from './components/Toasts';
import { ThemeProvider } from './theme/Theme';
import { SCREENS } from './screens';
import { ChangePassword, SignIn } from './pages/Auth';
import { Layout } from './pages/Layout';
import { ComingSoon, Home, NotFound } from './pages/Home';
import { Operations } from './pages/operations/Operations';
import { Settings } from './pages/operations/Settings';
import { Rutas, Zonas } from './pages/operations/RutasZonas';
import { CoveragePage, Lugares, NonWorkingDays, OtherCatalogs } from './pages/operations/Catalogs';
import { Operadores, UserList, Users } from './pages/users/Users';
import './styles.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Retrying does not fix a 4xx.
      retry: (attempts, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && attempts < 2,
    },
  },
});

/** Browser paths are user-visible, so they stay in Spanish. */
function App() {
  const { user, can } = useSession();

  if (!user) return <SignIn />;
  if (user.mustChangePassword) return <ChangePassword required />;

  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Home />} />
        <Route path="mi-contrasena" element={<ChangePassword required={false} />} />

        {can('operacion') && (
          <Route path="operacion" element={<Operations />}>
            <Route index element={<Navigate to="configuracion" replace />} />
            <Route path="configuracion" element={<Settings />} />
            <Route path="rutas" element={<Rutas />} />
            <Route path="zonas" element={<Zonas />} />
            <Route path="lugares" element={<Lugares />} />
            <Route path="dias-inhabiles" element={<NonWorkingDays />} />
            <Route path="cobertura" element={<CoveragePage />} />
            <Route path="catalogos" element={<OtherCatalogs />} />
          </Route>
        )}

        {can('usuarios') && (
          <Route path="usuarios" element={<Users />}>
            <Route index element={<Navigate to="lista" replace />} />
            <Route path="lista" element={<UserList />} />
            <Route path="operadores" element={<Operadores />} />
          </Route>
        )}

        {SCREENS.filter((s) => s.week && can(s.key)).map((s) => (
          <Route key={s.key} path={s.path.slice(1)} element={<ComingSoon screen={s} />} />
        ))}

        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <SessionProvider>
            <BrowserRouter>
              <App />
            </BrowserRouter>
          </SessionProvider>
        </ToastProvider>
      </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>,
);

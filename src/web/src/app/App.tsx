import { QueryClient } from '@tanstack/react-query';
import { Navigate, Route, Routes } from 'react-router';
import { ApiError } from '../shared/api/client';
import { useSession } from '../shared/session/Session';
import { ChangePassword, SignIn } from '../features/auth';
import { Layout } from './Layout';
import { ComingSoon, Home, NotFound } from '../features/home';
import { CoveragePage, Lugares, NonWorkingDays, Operations, OtherCatalogs, PostalCodes, Rutas, Settings, Zonas } from '../features/operacion';
import { Operadores, RolesPermissions, UserList, Users } from '../features/usuarios';
import { Bitacora } from '../features/bitacora';
import { Agendar } from '../features/agenda';
import { Clientes } from '../features/crm';
import { ParadasFijas } from '../features/paradasFijas';
import { PanelDelDia } from '../features/panel';
import { Unidades } from '../features/unidades';
import { Promociones } from '../features/promociones';
import { WhatsAppYMetas } from '../features/avisos';
import { Kpis } from '../features/kpis';
import { EvidenciasYCaja } from '../features/evidencias';
import { ClientesYFuentes } from '../features/clientesFuentes';
import { VistaDelChofer } from '../features/chofer';
import { Rutas as RutasDelDia } from '../features/rutas';
import { Pedidos } from '../features/pedidos';
import { TicketsEnPlanta } from '../features/planta';
import { SCREENS } from './screens';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Retrying does not fix a 4xx.
      retry: (attempts, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && attempts < 2,
    },
  },
});

/** Browser paths are user-visible, so they stay in Spanish. */
export function App() {
  const { user, can } = useSession();
  // The general bitácora is for SuperUser and Administración (the API checks it again).
  const isAdmin = user?.role === 'SuperUser' || user?.role === 'Administracion';

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
            <Route path="codigos-postales" element={<PostalCodes />} />
            <Route path="catalogos" element={<OtherCatalogs />} />
          </Route>
        )}

        {can('usuarios') && (
          <Route path="usuarios" element={<Users />}>
            <Route index element={<Navigate to="lista" replace />} />
            <Route path="lista" element={<UserList />} />
            <Route path="operadores" element={<Operadores />} />
            {isAdmin && <Route path="bitacora" element={<Bitacora />} />}
          </Route>
        )}

        {can('roles-y-permisos') && <Route path="roles-y-permisos" element={<RolesPermissions />} />}

        {can('agendar') && <Route path="agendar" element={<Agendar />} />}
        {can('clientes-crm') && <Route path="clientes" element={<Clientes />} />}
        {can('paradas-fijas') && <Route path="paradas-fijas" element={<ParadasFijas />} />}
        {can('panel-del-dia') && <Route path="panel-del-dia" element={<PanelDelDia />} />}
        {can('unidades') && <Route path="unidades" element={<Unidades />} />}
        {can('promociones') && <Route path="promociones" element={<Promociones />} />}
        {can('whatsapp-y-metas') && <Route path="whatsapp-y-metas" element={<WhatsAppYMetas />} />}
        {can('vista-del-chofer') && <Route path="vista-del-chofer" element={<VistaDelChofer />} />}
        {can('rutas') && <Route path="rutas" element={<RutasDelDia />} />}
        {can('pedidos') && <Route path="pedidos" element={<Pedidos />} />}
        {can('tickets-en-planta') && <Route path="tickets-en-planta" element={<TicketsEnPlanta />} />}
        {can('kpis') && <Route path="kpis" element={<Kpis />} />}
        {can('evidencias-y-caja') && <Route path="evidencias-y-caja" element={<EvidenciasYCaja />} />}
        {can('clientes-y-fuentes') && <Route path="clientes-y-fuentes" element={<ClientesYFuentes />} />}

        {SCREENS.filter((s) => s.week && can(s.key)).map((s) => (
          <Route key={s.key} path={s.path.slice(1)} element={<ComingSoon screen={s} />} />
        ))}

        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}

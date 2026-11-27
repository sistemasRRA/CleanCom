import { NavLink, Outlet } from 'react-router';
import { PageHeader } from '../../../shared/components/Page';
import { useSession } from '../../../shared/session/Session';

export function Users() {
  const role = useSession().user?.role;
  // The general bitácora is for SuperUser and Administración (the API checks it again).
  const isAdmin = role === 'SuperUser' || role === 'Administracion';
  return (
    <>
      <PageHeader title="Usuarios" description="Usuarios de oficina y choferes, y operadores de la nómina." />
      <nav className="tabs">
        <NavLink to="lista">Usuarios</NavLink>
        <NavLink to="operadores">Operadores</NavLink>
        {isAdmin && <NavLink to="bitacora">Bitácora</NavLink>}
      </nav>
      <Outlet />
    </>
  );
}

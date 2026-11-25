import { NavLink, Outlet } from 'react-router';
import { PageHeader } from '../../../shared/components/Page';

export function Users() {
  return (
    <>
      <PageHeader title="Usuarios" description="Usuarios de oficina y choferes, y operadores de la nómina." />
      <nav className="tabs">
        <NavLink to="lista">Usuarios</NavLink>
        <NavLink to="operadores">Operadores</NavLink>
      </nav>
      <Outlet />
    </>
  );
}

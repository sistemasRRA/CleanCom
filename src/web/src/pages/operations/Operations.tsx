import { NavLink, Outlet } from 'react-router';
import { PageHeader } from '../../components/Common';

/** Tabs of the Operación screen. Paths are user-visible, so they stay in Spanish. */
const TABS = [
  { path: 'configuracion', name: 'Reglas' },
  { path: 'rutas', name: 'Rutas' },
  { path: 'zonas', name: 'Zonas' },
  { path: 'lugares', name: 'Tiendas y bodegas' },
  { path: 'dias-inhabiles', name: 'Días inhábiles' },
  { path: 'cobertura', name: 'Cobertura' },
  { path: 'codigos-postales', name: 'Códigos postales' },
  { path: 'catalogos', name: 'Otros catálogos' },
];

export function Operations() {
  return (
    <>
      <PageHeader
        title="Operación"
        description="Reglas que mueven la agenda y el ruteo. Los cambios se aplican de inmediato o desde la fecha que programes."
      />
      <nav className="tabs">
        {TABS.map((t) => (
          <NavLink key={t.path} to={t.path}>
            {t.name}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </>
  );
}

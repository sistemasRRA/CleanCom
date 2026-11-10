import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router';
import { useSession } from '../session/Session';
import { SCREENS } from '../screens';

/** Side menu by permissions and content area. */
export function Layout() {
  const { user, can, logout } = useSession();
  const [open, setOpen] = useState(false);
  const location = useLocation();
  const [lastPath, setLastPath] = useState(location.pathname);
  if (lastPath !== location.pathname) {
    // Close the mobile menu after navigating.
    setLastPath(location.pathname);
    setOpen(false);
  }

  const visible = SCREENS.filter((s) => can(s.key));
  const groups = [...new Set(visible.map((s) => s.group))];

  return (
    <div className={`app${open ? ' menu-open' : ''}`}>
      <nav className="menu" aria-label="Menú principal">
        <div className="menu__brand">
          Clean<span>Com</span>
        </div>
        <NavLink to="/" end>
          Inicio
        </NavLink>
        {groups.map((g) => (
          <div key={g}>
            <div className="menu__group">{g}</div>
            {visible
              .filter((s) => s.group === g)
              .map((s) => (
                <NavLink key={s.key} to={s.path} className={s.week ? 'pending' : undefined}>
                  {s.name}
                  {s.week && <span className="menu__week">sem. {s.week}</span>}
                </NavLink>
              ))}
          </div>
        ))}
        <div className="menu__footer">
          <div>{user?.fullName}</div>
          <div style={{ opacity: 0.7 }}>
            {user?.userName} · {user?.role}
          </div>
          <NavLink to="/mi-contrasena">Mi contraseña</NavLink>
          <button type="button" onClick={() => void logout()}>
            Cerrar sesión
          </button>
        </div>
      </nav>
      <div>
        <div className="mobile-bar">
          <button type="button" className="small" onClick={() => setOpen((o) => !o)} aria-label="Abrir menú">
            ☰
          </button>
          <strong>CleanCom</strong>
        </div>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router';
import { useSession } from '../session/Session';
import { SCREENS } from '../screens';
import { Logo } from '../components/Brand';
import { useTheme } from '../theme/Theme';

const COLLAPSED_KEY = 'cleancom.menuCollapsed';

function readCollapsed() {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === '1';
  } catch {
    return false;
  }
}

function ThemeIcon({ theme }: { theme: 'light' | 'dark' }) {
  // Shows the mode the button switches to: a moon in light mode, a sun in dark mode.
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      {theme === 'light' ? (
        <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" />
      ) : (
        <>
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
        </>
      )}
    </svg>
  );
}

function Chevron({ direction }: { direction: 'left' | 'right' }) {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true">
      <path
        d={direction === 'left' ? 'M15 5l-7 7 7 7' : 'M9 5l7 7-7 7'}
        fill="none"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Side menu by permissions and content area. On desktop the menu collapses to a narrow rail. */
export function Layout() {
  const { user, can, logout } = useSession();
  const { theme, toggleTheme } = useTheme();
  const [open, setOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const location = useLocation();
  const [lastPath, setLastPath] = useState(location.pathname);
  if (lastPath !== location.pathname) {
    // Close the mobile menu after navigating.
    setLastPath(location.pathname);
    setOpen(false);
  }

  function toggleCollapsed() {
    setCollapsed((c) => {
      try {
        localStorage.setItem(COLLAPSED_KEY, c ? '0' : '1');
      } catch {
        // Storage blocked: the choice lasts only for this visit.
      }
      return !c;
    });
  }

  const visible = SCREENS.filter((s) => can(s.key));
  const groups = [...new Set(visible.map((s) => s.group))];
  const toggleLabel = collapsed ? 'Mostrar menú' : 'Ocultar menú';
  const themeLabel = theme === 'dark' ? 'Modo claro' : 'Modo oscuro';

  return (
    <div className={`app${open ? ' menu-open' : ''}${collapsed ? ' menu-collapsed' : ''}`}>
      <nav className="menu" aria-label="Menú principal">
        <div className="menu__top">
          <NavLink to="/" end className="menu__brand" aria-label="CleanCom, inicio">
            <Logo tone="light" className="menu__logo-full" />
            <Logo tone="light" variant="mark" className="menu__logo-mark" />
          </NavLink>
          <button
            type="button"
            className="menu__toggle"
            onClick={toggleCollapsed}
            aria-label={toggleLabel}
            aria-expanded={!collapsed}
            data-tooltip={toggleLabel}
          >
            <Chevron direction={collapsed ? 'right' : 'left'} />
          </button>
        </div>
        <div className="menu__body">
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
              {user?.userName} · {user?.roleName}
            </div>
            <NavLink to="/mi-contrasena">Mi contraseña</NavLink>
            <button type="button" onClick={() => void logout()}>
              Cerrar sesión
            </button>
          </div>
        </div>
        <div className="menu__bottom">
          <button
            type="button"
            className="menu__theme"
            onClick={toggleTheme}
            aria-label={themeLabel}
            data-tooltip={themeLabel}
          >
            <ThemeIcon theme={theme} />
            <span className="menu__theme-label">{themeLabel}</span>
          </button>
        </div>
      </nav>
      <div>
        <div className="mobile-bar">
          <button type="button" className="small" onClick={() => setOpen((o) => !o)} aria-label="Abrir menú">
            ☰
          </button>
          <Logo tone="dark" className="mobile-bar__logo" />
        </div>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

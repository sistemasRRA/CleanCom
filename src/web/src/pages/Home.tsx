import { Link } from 'react-router';
import { useSession } from '../session/Session';
import { SCREENS, type MenuScreen } from '../screens';
import { PageHeader } from '../components/Common';

const DESCRIPTIONS: Record<string, string> = {
  operacion: 'Reglas de agenda y ruteo, rutas, zonas, lugares, días inhábiles y cobertura.',
  usuarios: 'Usuarios de oficina y choferes, y operadores de la nómina.',
};

export function Home() {
  const { user, can } = useSession();
  const available = SCREENS.filter((s) => !s.week && can(s.key));

  return (
    <>
      <PageHeader title={`Hola, ${user?.fullName ?? ''}`} description="Web de oficina de CleanCom." />
      {available.length === 0 ? (
        <div className="alert info">Tu rol todavía no tiene pantallas disponibles en la web de oficina.</div>
      ) : (
        <div className="grid">
          {available.map((s) => (
            <Link key={s.key} to={s.path} className="card" style={{ textDecoration: 'none', color: 'inherit' }}>
              <div className="card__body">
                <h2>{s.name}</h2>
                <p className="muted" style={{ marginTop: 6 }}>
                  {DESCRIPTIONS[s.key]}
                </p>
              </div>
            </Link>
          ))}
        </div>
      )}
    </>
  );
}

/** A screen the user may see but that has not been built yet. */
export function ComingSoon({ screen }: { screen: MenuScreen }) {
  return (
    <>
      <PageHeader title={screen.name} />
      <div className="card">
        <div className="empty">Esta pantalla se construye en la semana {screen.week} del plan de trabajo.</div>
      </div>
    </>
  );
}

export function NotFound() {
  return (
    <>
      <PageHeader title="No encontrada" />
      <div className="alert">
        Esta dirección no existe o no tienes permiso para verla. <Link to="/">Volver al inicio</Link>
      </div>
    </>
  );
}

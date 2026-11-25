import { type Fija, short } from '../types/fijas';

export function FijaState({ fija }: { fija: Fija }) {
  if (!fija.active) return <span className="badge">Inactiva</span>;
  if (fija.paused) return <span className="badge warning">Pausada</span>;
  return (
    <>
      <span className="badge success">Activa</span>
      {(fija.validFrom || fija.validUntil) && (
        <div className="muted">
          {fija.validFrom ? `desde ${short(fija.validFrom)}` : ''} {fija.validUntil ? `hasta ${short(fija.validUntil)}` : ''}
        </div>
      )}
    </>
  );
}

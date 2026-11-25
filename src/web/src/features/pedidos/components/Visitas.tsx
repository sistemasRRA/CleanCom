import { useState } from 'react';
import type { Schemas } from '../../../shared/api/client';
import { formatDateTime } from '../../../shared/utils/dates';
import { KIND, STATE, STATE_TONE, TURNO_NAME } from '../types/pedidos';
import { EvidenciaThumb } from './EvidenciaThumb';

/** Visits of the pedido by date; the removed ones (quitadas) fold away so they do not read as active. */
export function Visitas({ paradas }: { paradas: Schemas['PedidoParadaDto'][] }) {
  const [showRemoved, setShowRemoved] = useState(false);
  const sorted = [...paradas].sort((a, b) => a.date.localeCompare(b.date) || a.id - b.id);
  const active = sorted.filter((p) => p.active);
  const removed = sorted.filter((p) => !p.active);
  return (
    <>
      <h3 className="form-section">Visitas</h3>
      {active.length === 0 && <p className="muted">Sin visitas activas.</p>}
      <ul className="visit-list">
        {active.map((p) => (
          <VisitRow key={p.id} p={p} />
        ))}
      </ul>
      {removed.length > 0 && (
        <>
          <button type="button" className="link visit-list__toggle" onClick={() => setShowRemoved((x) => !x)} aria-expanded={showRemoved}>
            {showRemoved ? 'Ocultar quitadas' : `Ver quitadas (${removed.length})`}
          </button>
          {showRemoved && (
            <ul className="visit-list removed">
              {removed.map((p) => (
                <VisitRow key={p.id} p={p} />
              ))}
            </ul>
          )}
        </>
      )}
    </>
  );
}

function VisitRow({ p }: { p: Schemas['PedidoParadaDto'] }) {
  const details = [
    p.arrivedAt && `Llegó ${formatDateTime(p.arrivedAt)}`,
    p.closedAt && `Cerró ${formatDateTime(p.closedAt)}`,
    p.garmentsCollected != null && `${p.garmentsCollected} prendas`,
    p.paymentMethod &&
      `Pagó ${p.paymentAmount?.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })} (${p.paymentMethod.toLowerCase()}, referencia)`,
  ].filter(Boolean);
  return (
    <li className="visit">
      <div className="visit__main">
        <strong>{KIND[p.kind] ?? p.kind}</strong>
        <span>{p.label}</span>
        <span className="muted">{p.turno ? TURNO_NAME[p.turno] : '—'}</span>
        <span className="muted">Ruta {p.rutaCode}</span>
        <span className={`badge ${p.active ? STATE_TONE[p.state] : ''}`}>{p.active ? STATE[p.state] : 'Quitada'}</span>
      </div>
      {details.length > 0 && <div className="visit__details">{details.join(' · ')}</div>}
      {p.closeNote && (
        <div className={`visit__note${p.state === 'NoRealizada' ? ' danger' : ''}`}>
          {p.state === 'NoRealizada' ? 'Motivo' : 'Notas'}: {p.closeNote}
        </div>
      )}
      {p.evidence && p.evidence.length > 0 && (
        <div className="evidencias">
          {p.evidence.map((e) => (
            <EvidenciaThumb key={e.id} id={e.id} kind={e.kind} distance={e.distanceMeters} />
          ))}
        </div>
      )}
    </li>
  );
}

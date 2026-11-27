import { useState } from 'react';
import { PageHeader } from '../../../shared/components/Page';
import { CortesTab } from '../components/CortesTab';
import { VisitasTab } from '../components/VisitasTab';
import '../evidencias.css';

type Tab = 'visitas' | 'cortes';

/**
 * Evidencias y caja (docs/pantallas/oficina/evidencias-y-caja.md): every visit with its photo, ticket and payment of
 * reference, and the corte de caja of each chofer. Everything is calculated in the API.
 */
export function EvidenciasYCaja() {
  const [tab, setTab] = useState<Tab>('visitas');
  return (
    <>
      <PageHeader title="Evidencias y caja" description="Visitas con su foto, ticket y cobro de referencia, y el corte de caja de cada chofer.">
        <div className="segmented" role="group" aria-label="Vista">
          <button type="button" className={`segmented__option${tab === 'visitas' ? ' selected' : ''}`} onClick={() => setTab('visitas')}>
            Visitas
          </button>
          <button type="button" className={`segmented__option${tab === 'cortes' ? ' selected' : ''}`} onClick={() => setTab('cortes')}>
            Corte de caja
          </button>
        </div>
      </PageHeader>
      {tab === 'visitas' ? <VisitasTab /> : <CortesTab />}
    </>
  );
}

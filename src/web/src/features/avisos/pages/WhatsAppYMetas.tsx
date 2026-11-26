import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { avisosKey } from '../services/queryKeys';
import '../../../shared/api/client';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { AvisosTable } from '../components/AvisosTable';
import { Plantillas } from '../components/Plantillas';
import { Metas } from '../../kpis';
import { avisosApi } from '../services/avisosApi';

type Tab = 'por-mandar' | 'historial' | 'plantillas' | 'metas';

/**
 * WhatsApp y metas (docs/pantallas/oficina/whatsapp-y-metas.md): the avisos to send by hand while WhatsApp is not
 * connected, the log, the templates per event and the goals of the KPIs. Which aviso goes and by which channel is decided by the API.
 */
export function WhatsAppYMetas() {
  const [tab, setTab] = useState<Tab>('por-mandar');
  const avisos = useQuery({
    queryKey: [...avisosKey, tab],
    queryFn: () => avisosApi.list({ estado: tab === 'por-mandar' ? 'ParaMandarAMano' : undefined }),
    enabled: tab === 'por-mandar' || tab === 'historial',
    refetchInterval: 30_000,
  });
  const toSend = avisos.data?.toSendByHand;

  return (
    <>
      <PageHeader
        title="WhatsApp y metas"
        description="Avisos al invitado por evento: push si tiene la app; si no, WhatsApp si lo acepta. Metas de los KPIs por indicador."
      />
      {avisos.data && !avisos.data.whatsAppConnected && (
        <div className="alert" style={{ marginBottom: 16 }}>
          WhatsApp todavía no está conectado (faltan la cuenta de Meta y las plantillas aprobadas): cada aviso queda en "Por mandar" y se manda con un
          clic desde WhatsApp Web. {!avisos.data.pushConnected && 'Los avisos push se registran sin enviarse (modo de prueba).'}
        </div>
      )}
      <nav className="tabs">
        {(
          [
            ['por-mandar', `Por mandar${toSend ? ` (${toSend})` : ''}`],
            ['historial', 'Historial'],
            ['plantillas', 'Plantillas'],
            ['metas', 'Metas'],
          ] as [Tab, string][]
        ).map(([key, name]) => (
          <button key={key} type="button" className={tab === key ? 'selected' : ''} onClick={() => setTab(key)}>
            {name}
          </button>
        ))}
      </nav>
      {(tab === 'por-mandar' || tab === 'historial') && (
        <QueryState loading={avisos.isPending} error={avisos.error}>
          <AvisosTable avisos={avisos.data?.avisos ?? []} toSend={tab === 'por-mandar'} />
        </QueryState>
      )}
      {tab === 'plantillas' && <Plantillas />}
      {tab === 'metas' && <Metas />}
    </>
  );
}

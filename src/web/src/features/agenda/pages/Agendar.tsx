import { Link, useSearchParams } from 'react-router';
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSession } from '../../../shared/session/Session';
import '../../../shared/api/client';
import { PageHeader, QueryState } from '../../../shared/components/Page';
import { InvitadoCard, InvitadoForm, InvitadoSearch, invitadosApi } from '../../invitados';
import { AgendarDialog } from '../components/AgendarDialog';
import { Schedule } from '../components/Schedule';
import { AgendaBoard } from '../components/AgendaBoard';

export function Agendar() {
  const canEdit = useSession().can('agendar', 'edit');
  // "Agendar" from the CRM opens the screen with the invitado chosen (/agendar?invitado=12).
  const [params] = useSearchParams();
  const fromCrm = Number(params.get('invitado'));
  const [invitadoId, setInvitadoId] = useState<number | null>(Number.isInteger(fromCrm) && fromCrm > 0 ? fromCrm : null);
  const [direccionId, setDireccionId] = useState<number | null>(null);
  const [capturing, setCapturing] = useState(false);

  const invitado = useQuery({
    queryKey: ['invitados', invitadoId],
    queryFn: () => invitadosApi.get(invitadoId!),
    enabled: invitadoId !== null,
  });

  // Select the first address when the invitado changes or the chosen one disappears.
  useEffect(() => {
    const list = invitado.data?.addresses ?? [];
    if (!list.some((d) => d.id === direccionId)) setDireccionId(list[0]?.id ?? null);
  }, [invitado.data, direccionId]);

  function choose(id: number | null) {
    setInvitadoId(id);
    setDireccionId(null);
    setCapturing(false);
  }

  return (
    <div className="stack">
      <PageHeader title="Agendar" description="Captura de una recolección por teléfono o WhatsApp. La fecha la calcula el motor de agenda del API.">
        {params.get('invitado') && (
          <Link to="/clientes" className="button">
            ← Volver a Clientes (CRM)
          </Link>
        )}
      </PageHeader>
      <div className="agendar-search">
        <InvitadoSearch
          onChoose={choose}
          onNew={() => {
            choose(null);
            setCapturing(true);
          }}
          canEdit={canEdit}
        />
      </div>
      {capturing && <InvitadoForm onClose={() => setCapturing(false)} onSaved={(i) => choose(i.id)} />}
      {invitadoId !== null && (
        <AgendarDialog key={invitadoId} onClose={() => choose(null)}>
          <QueryState loading={invitado.isPending} error={invitado.error}>
            {invitado.data && (
              <div className="agendar">
                <InvitadoCard
                  invitado={invitado.data}
                  direccionId={direccionId}
                  onDireccion={setDireccionId}
                  onRemoved={() => choose(null)}
                  canEdit={canEdit}
                />
                {direccionId !== null ? (
                  <Schedule invitado={invitado.data} direccionId={direccionId} canEdit={canEdit} onScheduled={() => choose(null)} />
                ) : (
                  <section className="card">
                    <div className="empty">Agrega una dirección para ver cómo se agenda.</div>
                  </section>
                )}
              </div>
            )}
          </QueryState>
        </AgendarDialog>
      )}
      <AgendaBoard />
    </div>
  );
}

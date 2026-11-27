import { useQuery } from '@tanstack/react-query';
import { Dialog } from '../../../shared/components/Dialog';
import { EvidenciaThumb } from '../../../shared/components/EvidenciaThumb';
import { QueryState } from '../../../shared/components/Page';
import { clock, shortDay } from '../../../shared/utils/dates';
import { evidenciasApi } from '../services/evidenciasApi';
import { KIND } from '../types/evidencias';

/** Galería del pedido: the photos and signature of each of its visits, recolección and entrega (GET /pedidos/{id}/evidencias). */
export function GaleriaPedido({ pedidoId, onClose }: { pedidoId: number; onClose: () => void }) {
  const galeria = useQuery({ queryKey: ['pedidos', pedidoId, 'evidencias'], queryFn: () => evidenciasApi.galeria(pedidoId) });
  const g = galeria.data;
  return (
    <Dialog title={g ? `Pedido ${pedidoId} · ${g.invitado}` : `Pedido ${pedidoId}`} onClose={onClose} onSubmit={onClose} submitText="Cerrar" hideCancel className="galeria-dialog">
      <QueryState loading={galeria.isPending} error={galeria.error}>
        {g?.visits.length === 0 && <div className="empty">Este pedido no tiene visitas.</div>}
        <div className="galeria">
          {g?.visits.map((v) => (
            <section key={v.paradaId} className="galeria__visita">
              <h3>
                {KIND[v.kind]} · {shortDay(v.date)} · Ruta {v.rutaCode}
                {v.closedAt && <span className="muted"> · cerrada {clock(v.closedAt)}</span>}
                {v.state === 'NoRealizada' && <span className="badge danger">No realizada</span>}
              </h3>
              {v.photos.length === 0 && v.signatureId == null ? (
                <p className="muted">Sin fotos.</p>
              ) : (
                <div className="evidencias">
                  {v.photos.map((p) => (
                    <EvidenciaThumb key={p.id} id={p.id} kind="Foto" distance={p.distanceMeters} outside={p.outsideRadius} />
                  ))}
                  {v.signatureId != null && <EvidenciaThumb id={v.signatureId} kind="Firma" />}
                </div>
              )}
            </section>
          ))}
        </div>
      </QueryState>
    </Dialog>
  );
}

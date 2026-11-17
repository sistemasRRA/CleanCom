using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;

namespace CleanCom.Domain.Routing;

/// <summary>What the chofer captures when closing a visit (all optional except where a rule says so).</summary>
public readonly record struct VisitCloseData(int? Garments, PaymentMethod? PaymentMethod, decimal? PaymentAmount, int Photos);

/// <summary>
/// Closing a visit (docs/pantallas/apps/chofer.md, Flujo del turno, step 5): recolección with prendas and photo,
/// entrega with photo, signature and reference payment, traslado to confirm, or not done with motivo and photo.
/// Visita.FotoObligatoria applies to every visit.
/// </summary>
public static class VisitClosing
{
    /// <summary>Throws when the visit cannot be closed now: already closed, or out of order.</summary>
    public static void EnsureCanClose(FlowParada target, IReadOnlyList<FlowParada> ordered, bool orderRequired)
    {
        switch (target.State)
        {
            case ParadaState.Realizada or ParadaState.NoRealizada:
                throw new DomainException("Esta visita ya se cerró.");
            case ParadaState.Pendiente:
                // Closing without "en camino" or "llegué" is allowed, with the same order rules.
                ChoferFlow.EnsureCanStart(target, ordered, orderRequired);
                return;
        }
    }

    /// <summary>Checks a visit done.</summary>
    public static void EnsureComplete(ParadaKind kind, bool hasPedido, VisitCloseData data, bool photoRequired)
    {
        if (photoRequired && data.Photos == 0)
        {
            throw new DomainException("Toma la foto de evidencia antes de cerrar la visita.");
        }
        if (kind == ParadaKind.Recoleccion && hasPedido && data.Garments is not > 0)
        {
            throw new DomainException("Escribe cuántas prendas recogiste.");
        }
        if (data.Garments is < 0 or > 500)
        {
            throw new DomainException("Las prendas deben estar entre 0 y 500.");
        }
        if ((data.PaymentMethod is null) != (data.PaymentAmount is null))
        {
            throw new DomainException("Para registrar el pago indica la forma y el monto.");
        }
        if (data.PaymentAmount is <= 0)
        {
            throw new DomainException("El monto del pago debe ser mayor a cero.");
        }
    }

    /// <summary>Checks a visit not done: motivo of the catalog or written, and photo when required.</summary>
    public static void EnsureNotDone(int? reasonId, string? note, int photos, bool photoRequired)
    {
        if (reasonId is null && string.IsNullOrWhiteSpace(note))
        {
            throw new DomainException("Indica el motivo por el que no se realizó la visita.");
        }
        if (photoRequired && photos == 0)
        {
            throw new DomainException("Toma la foto de evidencia (por ejemplo, de la puerta) antes de cerrar.");
        }
    }

    /// <summary>The etapa a pedido reaches when its visit is done: recolección → Recolectado, entrega → Entregado.</summary>
    public static PedidoEtapa? EtapaAfter(ParadaKind kind) => kind switch
    {
        ParadaKind.Recoleccion => PedidoEtapa.Recolectado,
        ParadaKind.Entrega => PedidoEtapa.Entregado,
        _ => null,
    };
}

using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

public enum EvidenceKind
{
    Foto,
    Firma,
}

/// <summary>
/// Photo or signature of a visit. The file lives on the server's disk (configurable path), never in SQL Server;
/// <see cref="FilePath"/> is relative to that folder. Never deleted.
/// </summary>
public class Evidence : Entity, IActivatable
{
    public bool Active { get; set; } = true;
    public int ParadaId { get; set; }
    public Parada? Parada { get; set; }
    public EvidenceKind Kind { get; set; }
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    /// <summary>When the chofer took it (UTC), which may be earlier than when it reached the API (sin señal).</summary>
    public DateTimeOffset TakenAt { get; set; }
    public NetTopologySuite.Geometries.Point? Location { get; set; }
    /// <summary>Meters from the parada where it was taken; null when unknown.</summary>
    public int? DistanceMeters { get; set; }
    /// <summary>Id the app gives each action, so a resend after losing signal is not saved twice.</summary>
    public string? LocalId { get; set; }
}

/// <summary>Forma de pago of the reference charge; the official charge is in the POS.</summary>
public enum PaymentMethod
{
    Efectivo,
    Tarjeta,
    Transferencia,
}

/// <summary>
/// What the chofer says the invitado paid at the visit (reference for the corte de caja; the POS is the official
/// record). Never deleted.
/// </summary>
public class ReferencePayment : Entity, IActivatable
{
    public bool Active { get; set; } = true;
    public int ParadaId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
}

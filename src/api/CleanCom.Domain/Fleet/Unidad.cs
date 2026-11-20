using CleanCom.Domain.Common;

namespace CleanCom.Domain.Fleet;

public enum UnidadState
{
    Activa,
    FueraDeServicio,
}

/// <summary>A delivery vehicle (docs/pantallas/oficina/unidades.md).</summary>
public class Unidad : Entity, IActivatable
{
    public string Plate { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public int Year { get; set; }
    /// <summary>Garments that fit; Unidad.CapacidadPrendas when null.</summary>
    public int? GarmentCapacity { get; set; }
    /// <summary>Last known odometer reading.</summary>
    public int Km { get; set; }
    public DateTimeOffset? KmUpdatedAt { get; set; }
    public UnidadState State { get; set; } = UnidadState.Activa;
    /// <summary>Does not verify (for example the trimotos): no verificación notice.</summary>
    public bool VerificationExempt { get; set; }
    /// <summary>Service every this many km; Unidad.ServicioCadaKm when null.</summary>
    public int? ServiceEveryKm { get; set; }
    /// <summary>Service every this many months; Unidad.ServicioCadaMeses when null.</summary>
    public int? ServiceEveryMonths { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;

    public List<UnidadDocument> Documents { get; set; } = [];
    public List<UnidadEvent> Events { get; set; } = [];
    public List<RecurringPayment> Payments { get; set; } = [];
}

public enum UnidadDocumentKind
{
    Seguro,
    TarjetaCirculacion,
    Verificacion,
    Comprobante,
    Otro,
}

/// <summary>
/// A document of the unidad: póliza, tarjeta de circulación, verificación or a receipt. The file lives on the server's
/// disk (Archivos:Ruta), never in SQL Server.
/// </summary>
public class UnidadDocument : Entity, IActivatable
{
    public int UnidadId { get; set; }
    public UnidadDocumentKind Kind { get; set; }
    /// <summary>When it starts (for a verificación, the day it was done).</summary>
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? Description { get; set; }
    /// <summary>Receipt of a logbook entry.</summary>
    public int? EventId { get; set; }
    public string? FilePath { get; set; }
    public string? ContentType { get; set; }
    public bool Active { get; set; } = true;
}

public enum UnidadEventKind
{
    Servicio,
    Reparacion,
    Falla,
    Pago,
    Kilometraje,
}

public enum FailureSeverity
{
    Baja,
    Media,
    Alta,
}

/// <summary>Logbook (bitácora) entry: service, repair, failure, payment or odometer reading.</summary>
public class UnidadEvent : Entity, IActivatable
{
    public int UnidadId { get; set; }
    public UnidadEventKind Kind { get; set; }
    public DateOnly Date { get; set; }
    public int? Km { get; set; }
    public decimal? Amount { get; set; }
    public string Description { get; set; } = "";
    /// <summary>Only for a falla.</summary>
    public FailureSeverity? Severity { get; set; }
    /// <summary>A falla stays open until this is set.</summary>
    public DateOnly? ResolvedOn { get; set; }
    /// <summary>The recurring payment this pago covers; its next date moves forward.</summary>
    public int? RecurringPaymentId { get; set; }
    public bool Active { get; set; } = true;
}

public enum PaymentFrequency
{
    Mensual,
    Bimestral,
    Trimestral,
    Semestral,
    Anual,
}

/// <summary>Tenencia or refrendo, credit or lease, insurance in installments.</summary>
public class RecurringPayment : Entity, IActivatable
{
    public int UnidadId { get; set; }
    public string Concept { get; set; } = "";
    public decimal Amount { get; set; }
    public PaymentFrequency Frequency { get; set; }
    public DateOnly NextDate { get; set; }
    public bool Active { get; set; } = true;
}

public enum FleetAlertEvent
{
    Verificacion,
    Seguro,
    PagoRecurrente,
    TarjetaCirculacion,
    Servicio,
    Falla,
}

/// <summary>
/// When and to whom a fleet notice goes (docs/03-reglas-de-negocio.md, Flota). Edited in Unidades
/// (GET|PUT /avisos-flota/reglas).
/// </summary>
public class FleetAlertRule : Entity
{
    public FleetAlertEvent Event { get; set; }
    /// <summary>Days before the date, comma separated ("30,7,0"); 0 is the day itself.</summary>
    public string DaysBefore { get; set; } = "";
    /// <summary>Emails or role names, comma separated.</summary>
    public string Recipients { get; set; } = "";
    /// <summary>Correo, Web and/or Push, comma separated.</summary>
    public string Channels { get; set; } = "";
}

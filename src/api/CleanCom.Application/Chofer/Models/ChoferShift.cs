using CleanCom.Domain.Fleet;
using CleanCom.Domain.Routing;

namespace CleanCom.Application.Chofer;

/// <summary>A unidad as the chofer sees it at the start of the shift.</summary>
/// <param name="Name">Brand and model ("Nissan NV200").</param>
/// <param name="Km">Last known odometer reading; the starting km cannot be lower.</param>
/// <param name="Issues">Out of service, open falla or expired documents, in Spanish (a warning; it does not block).</param>
public record ChoferUnidadDto(int Id, string Plate, string Name, int Km, IReadOnlyList<string> Issues);

/// <summary>Total of one forma de pago: recorded in the visits or declared at the end of the shift.</summary>
public record PaymentTotalDto(PaymentMethod Method, decimal Amount, int Count);

/// <summary>
/// The chofer's shift on the day shown (docs/pantallas/apps/chofer.md, Flujo del turno, steps 1 and 7).
/// </summary>
/// <param name="Unidad">The unidad the office assigned to the ruta that day in Rutas; null until then (no shift without it).</param>
/// <param name="Recorded">
/// Reference payments the chofer recorded in the visits of the shift, per forma de pago; the app shows them when
/// declaring the corte de caja.
/// </param>
/// <param name="Declared">What the chofer declared at the end of the shift (corte de caja); empty before.</param>
/// <param name="OpenParadas">Paradas not closed yet (pending, en camino or en sitio).</param>
/// <param name="MaxKmPerShift">Unidad.KmMaximoPorTurno: above it the API asks to confirm the reading.</param>
public record ChoferShiftDto(
    DateTimeOffset? StartedAt,
    int? StartKm,
    DateTimeOffset? EndedAt,
    int? EndKm,
    ChoferUnidadDto? Unidad,
    IReadOnlyList<PaymentTotalDto> Recorded,
    IReadOnlyList<PaymentTotalDto> Declared,
    int OpenParadas,
    int MaxKmPerShift);

/// <summary>Inicio de turno: the odometer reading of the unidad the office assigned.</summary>
/// <param name="Confirm">The chofer confirms a reading the API asked about (more than Unidad.KmMaximoPorTurno).</param>
/// <param name="LocalId">Id the app gives the action; a resend after losing signal is answered without an error.</param>
/// <param name="At">When it happened on the phone (it may sync later, sin señal); now when empty.</param>
public record StartShiftRequest(int Km, bool Confirm = false, string? LocalId = null, DateTimeOffset? At = null);

/// <summary>What the chofer says they collected with one forma de pago.</summary>
public record DeclaredPaymentRequest(PaymentMethod Method, decimal Amount);

/// <summary>Fin de turno: odometer reading and the declaration for the corte de caja.</summary>
/// <param name="Declared">Amount per forma de pago; a forma left out counts as nothing collected.</param>
/// <param name="Confirm">The chofer confirms a long shift or closing with paradas still open.</param>
public record EndShiftRequest(
    int Km,
    IReadOnlyList<DeclaredPaymentRequest> Declared,
    string? Notes = null,
    bool Confirm = false,
    string? LocalId = null,
    DateTimeOffset? At = null);

/// <summary>A falla of the unidad of the day, reported from the app with an optional photo.</summary>
public record ReportFailureRequest(string Description, FailureSeverity Severity, string? LocalId = null, DateTimeOffset? At = null);

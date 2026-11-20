using CleanCom.Domain.Fleet;

namespace CleanCom.Application.Fleet;

/// <param name="GarmentCapacity">Unidad.CapacidadPrendas when empty.</param>
/// <param name="ServiceEveryKm">Unidad.ServicioCadaKm when empty.</param>
/// <param name="ServiceEveryMonths">Unidad.ServicioCadaMeses when empty.</param>
public record SaveUnidadRequest(
    string Plate,
    string Brand,
    string Model,
    int Year,
    int? GarmentCapacity,
    int Km,
    int? ServiceEveryKm,
    int? ServiceEveryMonths,
    UnidadState State,
    string? Notes,
    bool Active = true);

/// <summary>A logbook entry.</summary>
/// <param name="Severity">Only for a falla, and required there.</param>
/// <param name="RecurringPaymentId">For a pago: the recurring payment it covers; its next date moves forward.</param>
/// <param name="Amount">For a pago of a recurring payment, its amount when empty.</param>
public record SaveUnidadEventRequest(
    UnidadEventKind Kind,
    DateOnly Date,
    int? Km,
    decimal? Amount,
    string? Description,
    FailureSeverity? Severity,
    int? RecurringPaymentId);

public record SaveRecurringPaymentRequest(string Concept, decimal Amount, PaymentFrequency Frequency, DateOnly NextDate, bool Active = true);

public record FleetAlertDto(FleetAlertEvent Event, FleetAlertLevel Level, string Message, DateOnly? DueDate);

/// <summary>A unidad's card in Unidades: data, alerts, service and the year's spending.</summary>
/// <param name="Pending">Alerts that need someone to act (everything except "por vencer").</param>
public record UnidadCardDto(
    int Id,
    string Plate,
    string Brand,
    string Model,
    int Year,
    int Km,
    DateTimeOffset? KmUpdatedAt,
    UnidadState State,
    bool Active,
    IReadOnlyList<FleetAlertDto> Alerts,
    bool Pending,
    DateOnly? LastServiceOn,
    int? LastServiceKm,
    DateOnly? NextServiceOn,
    int? NextServiceKm,
    decimal SpentThisYear,
    decimal? CostPerKm,
    DateOnly? InsuranceUntil,
    DateOnly? CirculationCardUntil,
    DateOnly? LastVerifiedOn,
    int OpenFailures);

public record UnidadDocumentDto(int Id, UnidadDocumentKind Kind, DateOnly? ValidFrom, DateOnly? ValidUntil, string? Description, int? EventId, bool HasFile, DateTimeOffset CreatedAt);

public record UnidadEventDto(int Id, UnidadEventKind Kind, DateOnly Date, int? Km, decimal? Amount, string Description,
    FailureSeverity? Severity, DateOnly? ResolvedOn, int? RecurringPaymentId);

public record RecurringPaymentDto(int Id, string Concept, decimal Amount, PaymentFrequency Frequency, DateOnly NextDate, bool Active);

/// <summary>Everything about a unidad: the card plus documents, logbook and recurring payments.</summary>
public record UnidadDetailDto(
    UnidadCardDto Card,
    int? GarmentCapacity,
    int? ServiceEveryKm,
    int? ServiceEveryMonths,
    string? Notes,
    IReadOnlyList<UnidadDocumentDto> Documents,
    IReadOnlyList<UnidadEventDto> Events,
    IReadOnlyList<RecurringPaymentDto> Payments);

/// <summary>Unidades and how many have something pending (the Panel del día shows only that number).</summary>
public record UnidadesDto(IReadOnlyList<UnidadCardDto> Unidades, int WithPending);

/// <param name="DaysBefore">Days before the date, comma separated ("30,7,0").</param>
/// <param name="Recipients">Emails or role names, comma separated.</param>
/// <param name="Channels">Correo, Web and/or Push, comma separated.</param>
public record FleetAlertRuleDto(FleetAlertEvent Event, string DaysBefore, string Recipients, string Channels);

namespace CleanCom.Application.Chofer;

/// <summary>Where the phone is; empty when the chofer did not give location permission.</summary>
/// <param name="LocalId">Id the app gives the action; a resend after losing signal is answered without an error.</param>
/// <param name="At">When it happened on the phone (it may sync later, sin señal); now when empty.</param>
public record ChoferLocationRequest(double? Lat, double? Lng, string? LocalId = null, DateTimeOffset? At = null);

/// <param name="ConfirmOutsideRadius">The chofer confirms arriving farther than Gps.ToleranciaMetros; the office sees it.</param>
public record ArriveRequest(double? Lat, double? Lng, bool ConfirmOutsideRadius, string? LocalId = null, DateTimeOffset? At = null);

/// <summary>
/// "Cerrar visita": what was done. Prendas for a recolección; forma and monto of the reference payment for an entrega.
/// The photos and signature are uploaded first (POST /chofer/evidencias, /chofer/evidencias/firma).
/// </summary>
/// <param name="LocalId">Id the app gives the action; a resend after losing signal is answered without closing twice.</param>
public record CloseVisitRequest(
    string? LocalId,
    int? Garments,
    string? Notes,
    CleanCom.Domain.Routing.PaymentMethod? PaymentMethod,
    decimal? PaymentAmount,
    double? Lat,
    double? Lng,
    DateTimeOffset? At = null);

/// <summary>"No realizada": motivo of the catalog (visitas no realizadas) and/or written, with its photo uploaded first.</summary>
public record NotDoneRequest(string? LocalId, int? ReasonId, string? Note, double? Lat, double? Lng, DateTimeOffset? At = null);

/// <summary>
/// The invitado's signature as strokes drawn on a canvas of <paramref name="Width"/> × <paramref name="Height"/>:
/// each stroke is a flat list x1, y1, x2, y2…. The API draws the image, so no file from the phone is stored as is.
/// </summary>
public record SignatureRequest(int ParadaId, string? LocalId, double Width, double Height, IReadOnlyList<IReadOnlyList<double>> Strokes, DateTimeOffset? TakenAt);

/// <summary>An uploaded photo or signature.</summary>
public record EvidenciaDto(int Id, int ParadaId, CleanCom.Domain.Routing.EvidenciaKind Kind, DateTimeOffset TakenAt, int? DistanceMeters);

public record ChoferReasonDto(int Id, string Text);

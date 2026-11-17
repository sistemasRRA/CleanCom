namespace CleanCom.Application.Chofer;

/// <summary>Where the phone is; empty when the chofer did not give location permission.</summary>
public record ChoferLocationRequest(double? Lat, double? Lng);

/// <param name="ConfirmOutsideRadius">The chofer confirms arriving farther than Gps.ToleranciaMetros; the office sees it.</param>
public record ArriveRequest(double? Lat, double? Lng, bool ConfirmOutsideRadius);

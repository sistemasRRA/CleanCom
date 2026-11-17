namespace CleanCom.Application.Agenda;

/// <summary>"No se entregó": motivo from the catalog (failed entregas) and/or text. The photo arrives with the chofer app.</summary>
public record NoEntregadoRequest(int? ReasonId, string? Note);

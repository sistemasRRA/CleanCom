namespace CleanCom.Application.Rutas;

/// <param name="UnidadId">Null: no unidad that day.</param>
/// <param name="Confirm">The office confirms a unidad out of service, with an open falla or an expired document.</param>
public record AssignUnidadRequest(int? UnidadId, bool Confirm = false);

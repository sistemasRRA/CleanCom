namespace CleanCom.Application.Rutas;

/// <param name="OperadorId">Null: the operador whose usual ruta it is.</param>
public record AssignOperadorRequest(int? OperadorId);

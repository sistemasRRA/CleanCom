namespace CleanCom.Application.Access;

/// <param name="OperadorId">Existing operador linked to the user (Chofer only).</param>
/// <param name="CreateOperador">Creates a new operador with the user's name and phone (Chofer only).</param>
public record CreateUserRequest(
    string FullName,
    string UserName,
    string? Email,
    string? Phone,
    string Role,
    string InitialPassword,
    int? OperadorId,
    bool CreateOperador,
    int? UsualRutaId);

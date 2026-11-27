using CleanCom.Domain.Common;

namespace CleanCom.Application.ChangeLogs;

/// <summary>Filters of the general bitácora; all optional.</summary>
/// <param name="Entity">Kind of record: Invitado, Address, Pedido, Ruta, Zona, KpiGoal, User, UserRole, RolePermission or Role.</param>
/// <param name="From">First day (Ciudad de México).</param>
/// <param name="To">Last day, included.</param>
public record ChangeLogQuery(string? Entity, int? EntityId, int? UserId, DateOnly? From, DateOnly? To);

/// <param name="Field">The field as in the code, to filter or group.</param>
/// <param name="Label">The field in Spanish.</param>
public record FieldChangeDto(string Field, string Label, string? Before, string? After);

/// <summary>One change: who and when, which record and its fields before and after.</summary>
/// <param name="UserName">Who did it; null when it was the system (a job, the app's own process).</param>
/// <param name="EntityLabel">The kind of record in Spanish.</param>
public record ChangeLogDto(
    int Id,
    DateTimeOffset At,
    string? UserName,
    string EntityType,
    string EntityLabel,
    int EntityId,
    ChangeAction Action,
    IReadOnlyList<FieldChangeDto> Changes);

public record ChangeLogPageDto(IReadOnlyList<ChangeLogDto> Rows, int RowCount, int Page, int PageSize);

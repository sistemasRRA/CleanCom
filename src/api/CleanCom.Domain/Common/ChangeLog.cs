using System.Globalization;

namespace CleanCom.Domain.Common;

/// <summary>What happened to a record in the change log (bitácora).</summary>
public enum ChangeAction
{
    Alta,
    Cambio,
    /// <summary>Baja lógica (Active true → false), or a link removed (a user's role, a permission).</summary>
    Baja,
    /// <summary>Active false → true.</summary>
    Reactivacion,
}

/// <summary>One field that changed: its value before and after, as text.</summary>
public record FieldChange(string Field, string? Before, string? After);

/// <summary>
/// One entry of the change log (docs/03-reglas-de-negocio.md, Bitácora de cambios): who (<see cref="Entity.CreatedBy"/>),
/// when (<see cref="Entity.CreatedAt"/>) and what changed in a key record. Only inserted, never changed.
/// </summary>
public class ChangeLog : Entity
{
    /// <summary>Kind of record, for example "Invitado", "Address", "Pedido".</summary>
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    /// <summary>The record it belongs to (an address to its invitado, a zona to its ruta), to read them together.</summary>
    public string? OwnerType { get; set; }
    public int? OwnerId { get; set; }
    public ChangeAction Action { get; set; }
    /// <summary>The fields that changed, as JSON (<see cref="FieldChange"/> list).</summary>
    public string Changes { get; set; } = "[]";
}

/// <summary>Rules of the change log: which action a change is and how values are written.</summary>
public static class ChangeLogRules
{
    /// <summary>Longest value kept per field; longer texts are cut with "…".</summary>
    public const int MaxValueLength = 300;

    /// <summary>
    /// Fields never logged: the audit stamps themselves and technical ones that change on their own (sign-in data,
    /// security stamps). A change made only of these is not logged.
    /// </summary>
    public static readonly IReadOnlySet<string> IgnoredFields = new HashSet<string>
    {
        nameof(Entity.Id), nameof(Entity.CreatedAt), nameof(Entity.CreatedBy), nameof(Entity.ModifiedAt), nameof(Entity.ModifiedBy),
        "LastAccess", "SecurityStamp", "ConcurrencyStamp", "PasswordHash", "AccessFailedCount", "LockoutEnd",
        "NormalizedUserName", "NormalizedEmail",
    };

    /// <summary>
    /// The action of a modified record: a change of Active alone is a baja or a reactivación; with other fields it is
    /// still the baja or reactivación (the fields go with it). Example: Active true → false → Baja.
    /// </summary>
    public static ChangeAction ModifiedAction(IEnumerable<FieldChange> changes)
    {
        var active = changes.FirstOrDefault(c => c.Field == nameof(IActivatable.Active));
        return active switch
        {
            { Before: "Sí", After: "No" } => ChangeAction.Baja,
            { Before: "No", After: "Sí" } => ChangeAction.Reactivacion,
            _ => ChangeAction.Cambio,
        };
    }

    /// <summary>
    /// A value as the office reads it: booleans as Sí/No, dates in ISO (the screen formats them), enums by name and
    /// long texts cut. Null stays null.
    /// </summary>
    public static string? Format(object? value)
    {
        var text = value switch
        {
            null => null,
            bool b => b ? "Sí" : "No",
            DateTimeOffset d => d.ToString("O", CultureInfo.InvariantCulture),
            DateTime d => d.ToString("O", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
        return text is { Length: > MaxValueLength } ? text[..(MaxValueLength - 1)] + "…" : text;
    }

    /// <summary>The fields that really changed (ignoring the technical ones and equal values).</summary>
    public static IReadOnlyList<FieldChange> Diff(IEnumerable<(string Field, object? Before, object? After)> values) =>
        values
            .Where(v => !IgnoredFields.Contains(v.Field))
            .Select(v => new FieldChange(v.Field, Format(v.Before), Format(v.After)))
            .Where(c => c.Before != c.After)
            .ToList();
}

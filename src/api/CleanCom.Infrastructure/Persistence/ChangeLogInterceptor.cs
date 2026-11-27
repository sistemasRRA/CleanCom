using System.Text.Json;
using CleanCom.Domain.Access;
using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Kpis;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;
using CleanCom.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetTopologySuite.Geometries;

namespace CleanCom.Infrastructure.Persistence;

/// <summary>
/// Writes the bitácora (<see cref="ChangeLog"/>) of the key records on every save: what changed, before and after.
/// Who and when are stamped by <see cref="AuditInterceptor"/>. Modified and removed records are read before the save
/// (their original values are lost after it); added ones after it, when their id exists. The entries are saved right
/// after, in the same transaction when the caller opened one. Changes made with ExecuteUpdate are not logged.
/// </summary>
public class ChangeLogInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Logged records: their name in the bitácora and the record they belong to.</summary>
    private static readonly Dictionary<Type, (string Name, Func<Func<string, object?>, (string Type, int Id)?> Owner)> Logged = new()
    {
        [typeof(Invitado)] = ("Invitado", _ => null),
        [typeof(Address)] = ("Address", v => ("Invitado", (int)v(nameof(Address.InvitadoId))!)),
        [typeof(Pedido)] = ("Pedido", _ => null),
        [typeof(Ruta)] = ("Ruta", _ => null),
        [typeof(Zona)] = ("Zona", v => v(nameof(Zona.RutaId)) is int ruta ? ("Ruta", ruta) : null),
        [typeof(KpiGoal)] = ("KpiGoal", _ => null),
        [typeof(User)] = ("User", _ => null),
        [typeof(IdentityUserRole<int>)] = ("UserRole", v => ("User", (int)v(nameof(IdentityUserRole<int>.UserId))!)),
        [typeof(RolePermission)] = ("RolePermission", v => ("Role", (int)v(nameof(RolePermission.RoleId))!)),
    };

    private sealed record Pending(EntityEntry Entry, string Name, ChangeLog? Ready);

    private List<Pending> pending = [];
    private bool writing;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is { } context && Complete() is { Count: > 0 } logs)
        {
            context.Set<ChangeLog>().AddRange(logs);
            Write(() => context.SaveChanges());
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && Complete() is { Count: > 0 } logs)
        {
            context.Set<ChangeLog>().AddRange(logs);
            writing = true;
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                writing = false;
            }
        }
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => pending = [];

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        pending = [];
        return Task.CompletedTask;
    }

    private void Write(Action save)
    {
        writing = true;
        try
        {
            save();
        }
        finally
        {
            writing = false;
        }
    }

    private void Capture(DbContext? context)
    {
        if (context is null || writing)
        {
            return;
        }
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (!Logged.TryGetValue(entry.Metadata.ClrType, out var logged))
            {
                continue;
            }
            switch (entry.State)
            {
                case EntityState.Added:
                    // Its id (and the ids of new records it points to) exist after the save.
                    pending.Add(new Pending(entry, logged.Name, null));
                    break;
                case EntityState.Modified:
                    var changes = ChangeLogRules.Diff(entry.Properties
                        .Where(p => p.IsModified)
                        .Select(p => (p.Metadata.Name, Value(p.OriginalValue), Value(p.CurrentValue))));
                    if (changes.Count > 0)
                    {
                        pending.Add(new Pending(entry, logged.Name, Log(entry, logged.Name, ChangeLogRules.ModifiedAction(changes), changes, v => entry.Property(v).CurrentValue)));
                    }
                    break;
                case EntityState.Deleted:
                    pending.Add(new Pending(entry, logged.Name, Log(entry, logged.Name, ChangeAction.Baja, Values(entry, original: true), v => entry.Property(v).OriginalValue)));
                    break;
            }
        }
    }

    /// <summary>The entries of this save, the added ones completed now that they have an id.</summary>
    private List<ChangeLog> Complete()
    {
        if (writing || pending.Count == 0)
        {
            return [];
        }
        var done = pending;
        pending = [];
        return done
            .Select(p => p.Ready ?? Log(p.Entry, p.Name, ChangeAction.Alta, Values(p.Entry, original: false), v => p.Entry.Property(v).CurrentValue))
            .ToList();
    }

    /// <summary>Every field with a value, for an alta (after) or a removal (before).</summary>
    private static IReadOnlyList<FieldChange> Values(EntityEntry entry, bool original) =>
        ChangeLogRules.Diff(entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .Select(p => original
                ? (p.Metadata.Name, Value(p.OriginalValue), (object?)null)
                : (p.Metadata.Name, (object?)null, Value(p.CurrentValue))));

    private static ChangeLog Log(EntityEntry entry, string name, ChangeAction action, IReadOnlyList<FieldChange> changes, Func<string, object?> value)
    {
        var owner = Logged[entry.Metadata.ClrType].Owner(value);
        // A user's role has no id of its own: it is logged under the user.
        var id = entry.Entity is IdentityUserRole<int> userRole ? userRole.UserId : (int)value("Id")!;
        return new ChangeLog
        {
            EntityType = name,
            EntityId = id,
            OwnerType = owner?.Type,
            OwnerId = owner?.Id,
            Action = action,
            Changes = JsonSerializer.Serialize(changes, Json),
        };
    }

    /// <summary>Map values as text: a point as "lat,lng", a polygon by its size.</summary>
    private static object? Value(object? value) => value switch
    {
        Point p => FormattableString.Invariant($"{p.Y:0.######},{p.X:0.######}"),
        // The area tells apart two polygons with the same number of points.
        Geometry g => FormattableString.Invariant($"Polígono de {g.NumPoints} puntos (área {g.Area:0.##########})"),
        _ => value,
    };
}

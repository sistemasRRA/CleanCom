using System.Collections.Concurrent;
using CleanCom.Application.Access;
using CleanCom.Domain.Access;
using CleanCom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCom.Infrastructure.Identity;

/// <summary>
/// Singleton cache of permissions per role. RoleService calls Invalidate after every change,
/// so the next request already sees it. The API runs as a single instance on the VPS.
/// </summary>
public class PermissionStore(IServiceScopeFactory scopes) : IPermissionStore
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _cache = new();

    public async Task<IReadOnlyList<string>> GetAsync(string role, CancellationToken ct)
    {
        if (Roles.HasLockedPermissions(role))
        {
            // Never read from the database: SuperUser can not lose access by accident.
            return [.. Permissions.FullOffice.Select(p => Permissions.ToClaim(p.Screen, p.Level))];
        }
        if (_cache.TryGetValue(role, out var cached))
        {
            return cached;
        }

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CleanComDbContext>();
        var rows = await (
                from p in db.RolePermissions.AsNoTracking()
                join r in db.Roles on p.RoleId equals r.Id
                where r.Name == role
                orderby p.Screen
                select new { p.Screen, p.Level })
            .ToListAsync(ct);

        IReadOnlyList<string> permissions = [.. rows.Select(p => Permissions.ToClaim(p.Screen, p.Level))];
        _cache[role] = permissions;
        return permissions;
    }

    public void Invalidate() => _cache.Clear();
}

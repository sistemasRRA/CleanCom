using System.Text.Json;
using CleanCom.Application.Access;
using CleanCom.Application.Common;
using CleanCom.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.ChangeLogs;

/// <summary>
/// Reads the bitácora (docs/03-reglas-de-negocio.md, Bitácora de cambios) as the office reads it: who, when, which
/// record and each field before and after, in Spanish, with the ids of rutas, zonas, roles and users as their names.
/// </summary>
public class ChangeLogService(ICleanComDbContext db, IUserService users, IRoleService roles)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The invitado's record and what belongs to it (their addresses), newest first.</summary>
    public async Task<ChangeLogPageDto> ForInvitadoAsync(int id, int page, int pageSize, CancellationToken ct)
    {
        if (!await db.Invitados.AnyAsync(i => i.Id == id, ct))
        {
            throw new NotFoundException($"El invitado {id} no existe.");
        }
        return await SearchAsync(new ChangeLogQuery("Invitado", id, null, null, null), page, pageSize, ct);
    }

    /// <summary>
    /// The general bitácora. With a kind of record (and its id) it also brings what belongs to it: a ruta with its
    /// zonas, a user with their role. <see cref="ChangeLogQuery.To"/> is included.
    /// </summary>
    public async Task<ChangeLogPageDto> SearchAsync(ChangeLogQuery q, int page, int pageSize, CancellationToken ct)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var logs = db.ChangeLogs.AsNoTracking();
        if (q.Entity is { Length: > 0 } entity)
        {
            logs = q.EntityId is { } entityId
                ? logs.Where(l => (l.EntityType == entity && l.EntityId == entityId) || (l.OwnerType == entity && l.OwnerId == entityId))
                : logs.Where(l => l.EntityType == entity || l.OwnerType == entity);
        }
        if (q.UserId is { } userId)
        {
            logs = logs.Where(l => l.CreatedBy == userId);
        }
        if (q.From is { } from)
        {
            var start = Domain.Time.BusinessTime.ToUtc(from, TimeOnly.MinValue);
            logs = logs.Where(l => l.CreatedAt >= start);
        }
        if (q.To is { } to)
        {
            var end = Domain.Time.BusinessTime.ToUtc(to.AddDays(1), TimeOnly.MinValue);
            logs = logs.Where(l => l.CreatedAt < end);
        }

        var count = await logs.CountAsync(ct);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, pages);
        var rows = await logs.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var parsed = rows.Select(r => (Log: r, Changes: JsonSerializer.Deserialize<List<FieldChange>>(r.Changes, Json) ?? [])).ToList();
        var names = await NamesAsync(parsed, ct);
        return new ChangeLogPageDto(
            parsed.Select(p => new ChangeLogDto(
                p.Log.Id,
                p.Log.CreatedAt,
                p.Log.CreatedBy is { } by ? names.Users.GetValueOrDefault(by) : null,
                p.Log.EntityType,
                ChangeLogLabels.Entity(p.Log.EntityType),
                p.Log.EntityId,
                p.Log.Action,
                p.Changes.Select(c => new FieldChangeDto(
                    c.Field,
                    ChangeLogLabels.Field(p.Log.EntityType, c.Field),
                    names.Resolve(c.Field, c.Before),
                    names.Resolve(c.Field, c.After))).ToList())).ToList(),
            count, page, pageSize);
    }

    /// <summary>Names for the ids that appear: who did it, and rutas, zonas, roles and responsables in the values.</summary>
    private async Task<Names> NamesAsync(List<(ChangeLog Log, List<FieldChange> Changes)> rows, CancellationToken ct)
    {
        IEnumerable<int> Ids(params string[] fields) => rows
            .SelectMany(r => r.Changes.Where(c => fields.Contains(c.Field)))
            .SelectMany(c => new[] { c.Before, c.After })
            .Select(v => int.TryParse(v, out var n) ? n : (int?)null)
            .OfType<int>();

        var userIds = rows.Select(r => r.Log.CreatedBy).OfType<int>().Concat(Ids("ResponsableUserId")).Distinct().ToList();
        var rutaIds = Ids("RutaId").Distinct().ToList();
        var zonaIds = Ids("ZonaId", "ZonaNucleoId").Distinct().ToList();
        var userNames = await users.NamesAsync(userIds, ct);
        var rutas = rutaIds.Count == 0 ? new Dictionary<int, string>()
            : await db.Rutas.AsNoTracking().Where(r => rutaIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => $"Ruta {r.Code}", ct);
        var zonas = zonaIds.Count == 0 ? new Dictionary<int, string>()
            : await db.Zonas.AsNoTracking().Where(z => zonaIds.Contains(z.Id)).ToDictionaryAsync(z => z.Id, z => $"Zona {z.Code}", ct);
        var roleNames = Ids("RoleId").Any() ? (await roles.ListAsync(ct)).ToDictionary(r => r.Id, r => r.Name) : new Dictionary<int, string>();
        return new Names(userNames, rutas, zonas, roleNames);
    }

    private sealed record Names(
        IReadOnlyDictionary<int, string> Users,
        IReadOnlyDictionary<int, string> Rutas,
        IReadOnlyDictionary<int, string> Zonas,
        IReadOnlyDictionary<int, string> Roles)
    {
        public string? Resolve(string field, string? value)
        {
            if (!int.TryParse(value, out var id))
            {
                return value;
            }
            var map = field switch
            {
                "ResponsableUserId" => Users,
                "RutaId" => Rutas,
                "ZonaId" or "ZonaNucleoId" => Zonas,
                "RoleId" => Roles,
                _ => null,
            };
            return map?.GetValueOrDefault(id) ?? value;
        }
    }
}

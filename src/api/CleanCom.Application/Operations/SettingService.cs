using CleanCom.Application.Common;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Operations;

public class SettingService(
    ICleanComDbContext db,
    IBusinessClock clock,
    IValidator<ChangeSettingRequest> validator) : ISettingService
{
    public async Task<IReadOnlyList<SettingDto>> ListAsync(CancellationToken ct)
    {
        var history = await db.Settings.AsNoTracking().ToListAsync(ct);
        var now = clock.UtcNow;

        return SettingCatalog.Definitions
            .Select(d =>
            {
                var current = SettingResolver.Current(history, d.Key, now);
                var scheduled = SettingResolver.Scheduled(history, d.Key, now);
                return new SettingDto(
                    d.Key,
                    d.Description,
                    d.Type,
                    d.PerRuta,
                    current?.Value ?? d.InitialValue,
                    current?.EffectiveFrom ?? DateTimeOffset.MinValue,
                    scheduled is null ? null : new ScheduledChangeDto(scheduled.Value, scheduled.EffectiveFrom));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<SettingHistoryDto>> HistoryAsync(string? key, CancellationToken ct)
    {
        var query = db.Settings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(key))
        {
            query = query.Where(s => s.Key == key);
        }

        var rows = await query.ToListAsync(ct);
        return rows
            .OrderBy(s => s.Key)
            .ThenByDescending(s => s.EffectiveFrom)
            .ThenByDescending(s => s.Id)
            .Select(ToHistory)
            .ToList();
    }

    public async Task<SettingHistoryDto> ChangeAsync(ChangeSettingRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var definition = SettingCatalog.Find(request.Key)!;
        var now = clock.UtcNow;
        var effectiveFrom = request.EffectiveFrom?.ToUniversalTime() ?? now;
        if (effectiveFrom < now.AddMinutes(-1))
        {
            throw new DomainException("La fecha de vigencia no puede estar en el pasado.");
        }

        var row = new Setting
        {
            Key = definition.Key,
            Type = definition.Type,
            Value = SettingValue.Normalize(definition.Type, request.Value),
            EffectiveFrom = effectiveFrom < now ? now : effectiveFrom,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };
        db.Settings.Add(row);
        await db.SaveChangesAsync(ct);
        return ToHistory(row);
    }

    public async Task<string> GetAsync(string key, int? rutaId, CancellationToken ct)
    {
        var history = await db.Settings.AsNoTracking().Where(s => s.Key == key).ToListAsync(ct);
        var ruta = rutaId is null ? null : await db.Rutas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rutaId, ct)
            ?? throw new NotFoundException($"La ruta {rutaId} no existe.");
        return SettingResolver.Resolve(history, key, clock.UtcNow, ruta);
    }

    private static SettingHistoryDto ToHistory(Setting s) =>
        new(s.Id, s.Key, s.Value, s.EffectiveFrom, s.CreatedAt, s.CreatedBy, s.Note);
}

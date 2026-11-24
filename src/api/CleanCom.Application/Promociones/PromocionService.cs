using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Common;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Promociones;
using CleanCom.Domain.Time;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Promociones;

/// <summary>
/// Promociones (docs/pantallas/oficina/promociones.md): sellos and cuponera with their numbers, granting a beneficio by
/// hand and marking one as used by its code. Campaigns arrive with notifications.
/// </summary>
public class PromocionService(
    ICleanComDbContext db,
    IBusinessClock clock,
    ISettingService settings,
    BeneficioService beneficios,
    IValidator<SavePromocionRequest> validator)
{
    public async Task<PromocionesBoardDto> BoardAsync(CancellationToken ct)
    {
        var promociones = await db.Promociones.AsNoTracking()
            .OrderBy(p => p.Programa).ThenByDescending(p => p.Active).ThenBy(p => p.Name).ToListAsync(ct);
        var stats = await db.Beneficios.AsNoTracking().Where(b => b.Active)
            .GroupBy(b => b.PromocionId)
            .Select(g => new
            {
                PromocionId = g.Key,
                Granted = g.Count(),
                Used = g.Count(b => b.UsedAt != null),
                Reserved = g.Count(b => b.UsedAt == null && b.PedidoId != null),
            })
            .ToDictionaryAsync(x => x.PromocionId, ct);
        var lineas = await db.LineasServicio.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Name, ct);
        var cap = await settings.GetIntegerAsync(SettingKeys.PromocionesSelloCap, ct);

        int Granted(Promocion p) => stats.TryGetValue(p.Id, out var s) ? s.Granted : 0;
        int Used(Promocion p) => stats.TryGetValue(p.Id, out var s) ? s.Used : 0;
        var sellosGranted = promociones.Where(p => p.Programa == PromocionPrograma.Sellos).Sum(Granted);

        var list = promociones.Select(p =>
        {
            string? Name(int? id) => id is { } x ? lineas.GetValueOrDefault(x) : null;
            var granted = Granted(p);
            var sello = p.Programa == PromocionPrograma.Sellos;
            return new PromocionDto(p.Id, p.Programa, p.Name, p.LineaServicioId, Name(p.LineaServicioId), p.DiscountType, p.Value,
                p.MinimumAmount, p.PaymentMethod, p.Stackable, p.TriggerLineaServicioId, Name(p.TriggerLineaServicioId), p.Description, p.Condition,
                p.ValidityDays, p.Active, BeneficioRules.Mechanic(p, Name(p.LineaServicioId)), granted, Used(p),
                stats.TryGetValue(p.Id, out var s) ? s.Reserved : 0, BeneficioRules.RedemptionRate(granted, Used(p)),
                sello ? BeneficioRules.RedemptionRate(sellosGranted, granted) : null,
                sello && BeneficioRules.ExceedsShare(granted, sellosGranted, cap));
        }).ToList();

        var indicators = new List<ProgramaIndicatorDto>();
        foreach (var (programa, key) in new[]
                 {
                     (PromocionPrograma.Sellos, SettingKeys.PromocionesSellosGoal),
                     (PromocionPrograma.Cuponera, SettingKeys.PromocionesCuponeraGoal),
                 })
        {
            var of = promociones.Where(p => p.Programa == programa).ToList();
            var granted = of.Sum(Granted);
            var used = of.Sum(Used);
            var rate = BeneficioRules.RedemptionRate(granted, used);
            var goal = await settings.GetIntegerAsync(key, ct);
            indicators.Add(new ProgramaIndicatorDto(programa, granted, used, rate, goal, rate is { } r && r >= goal));
        }
        return new PromocionesBoardDto(indicators, list, cap);
    }

    public async Task<PromocionDto> SaveAsync(int? id, SavePromocionRequest r, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(r, ct);
        var promocion = id is null ? new Promocion() : await db.Promociones.FindAsync([id], ct)
            ?? throw new NotFoundException($"La promoción {id} no existe.");
        var name = r.Name.Trim();
        if (r.Active && await db.Promociones.AnyAsync(p => p.Active && p.Programa == r.Programa && p.Name == name && p.Id != promocion.Id, ct))
        {
            throw new ConflictException($"Ya existe la promoción {name}.");
        }
        var lineaIds = new[] { r.LineaServicioId, r.TriggerLineaServicioId }.OfType<int>().Distinct().ToList();
        if (await db.LineasServicio.CountAsync(l => lineaIds.Contains(l.Id), ct) != lineaIds.Count)
        {
            throw new DomainException("La línea de servicio no existe.");
        }

        promocion.Programa = r.Programa;
        promocion.Name = name;
        promocion.LineaServicioId = r.LineaServicioId;
        promocion.DiscountType = r.DiscountType;
        promocion.Value = r.DiscountType == DiscountType.ArticuloGratis ? 0 : r.Value;
        promocion.MinimumAmount = r.MinimumAmount;
        promocion.PaymentMethod = r.PaymentMethod;
        promocion.Stackable = r.Stackable;
        promocion.TriggerLineaServicioId = r.Programa == PromocionPrograma.Sellos ? r.TriggerLineaServicioId : null;
        promocion.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        promocion.Condition = string.IsNullOrWhiteSpace(r.Condition) ? null : r.Condition.Trim();
        promocion.ValidityDays = r.ValidityDays;
        promocion.Active = r.Active;
        if (id is null)
        {
            db.Promociones.Add(promocion);
        }
        await db.SaveChangesAsync(ct);
        return (await BoardAsync(ct)).Promociones.First(p => p.Id == promocion.Id);
    }

    /// <summary>A sello or cupón given by hand (Clientes › ficha). Over Promociones.TopeSello it is still granted, with a warning.</summary>
    public async Task<GrantBeneficioResultDto> GrantAsync(int invitadoId, GrantBeneficioRequest r, CancellationToken ct)
    {
        var invitado = await db.Invitados.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invitadoId && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {invitadoId} no existe.");
        if (!BeneficioRules.TakesPart(invitado.Type))
        {
            throw new DomainException("Las tiendas internas no reciben beneficios.");
        }
        var promocion = await db.Promociones.FirstOrDefaultAsync(p => p.Id == r.PromocionId && p.Active, ct)
            ?? throw new NotFoundException("La promoción no existe o está inactiva.");
        if (promocion.Programa == PromocionPrograma.Campana)
        {
            throw new DomainException("Las campañas se entregan al enviarlas.");
        }
        // "Hasta la entrega del pedido": it goes with the pedido the invitado has in process.
        Pedido? pedido = null;
        if (promocion.ValidityDays is null)
        {
            pedido = await db.Pedidos.AsNoTracking()
                .Where(p => p.InvitadoId == invitadoId && p.Active && p.Etapa != PedidoEtapa.Entregado && p.Etapa != PedidoEtapa.Cancelado)
                .OrderByDescending(p => p.RecoleccionDate).ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new DomainException($"{promocion.Name} vale hasta la entrega de un pedido y el invitado no tiene uno en proceso.");
        }
        var beneficio = await beneficios.AddAsync(invitadoId, promocion, BeneficioOrigin.Oficina, pedido?.Id, pedido?.EntregaDate, ct);
        await db.SaveChangesAsync(ct);

        string? warning = null;
        if (promocion.Programa == PromocionPrograma.Sellos)
        {
            var board = await BoardAsync(ct);
            if (board.Promociones.FirstOrDefault(p => p.Id == promocion.Id) is { OverCap: true } over)
            {
                warning = $"{promocion.Name} ya es el {over.SharePercent}% de los sellos otorgados (tope {board.SelloCap}%).";
            }
        }
        return new GrantBeneficioResultDto(await beneficios.ToDtoAsync(beneficio, ct), warning);
    }

    /// <summary>"Marcar como usado" by its code (office): only an unused beneficio that has not expired.</summary>
    public async Task<BeneficioDto> UseAsync(string code, CancellationToken ct)
    {
        var normalized = BeneficioRules.NormalizeCode(code ?? "");
        var beneficio = await db.Beneficios.Include(b => b.Promocion).FirstOrDefaultAsync(b => b.Code == normalized && b.Active, ct)
            ?? throw new NotFoundException($"No existe el beneficio {normalized}. Revisa el código.");
        BeneficioRules.EnsureCanUse(beneficio, clock.Today);
        beneficio.UsedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return await beneficios.ToDtoAsync(beneficio, ct);
    }
}

using System.Security.Cryptography;
using CleanCom.Application.Common;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Promociones;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Promociones;

/// <summary>
/// The life of a beneficio (docs/03-reglas-de-negocio.md, Promociones): granted when a recolección closes (cuponera on the
/// first service, sellos on the next ones), reserved when a service is asked for, used when its recolección closes and
/// freed if the pedido is cancelled. Office, Agenda, Pedidos and the chofer app all go through here.
/// </summary>
public class BeneficioService(ICleanComDbContext db, IBusinessClock clock)
{
    public async Task<IReadOnlyList<BeneficioDto>> ForInvitadoAsync(int invitadoId, CancellationToken ct) =>
        await ToDtosAsync(db.Beneficios.Where(b => b.InvitadoId == invitadoId && b.Active), ct);

    public async Task<IReadOnlyList<BeneficioDto>> ForPedidoAsync(int pedidoId, CancellationToken ct) =>
        await ToDtosAsync(db.Beneficios.Where(b => b.PedidoId == pedidoId && b.Active), ct);

    /// <summary>Beneficios reserved for (or used on) each pedido, for the chofer's ruta.</summary>
    public async Task<ILookup<int, BeneficioDto>> ForPedidosAsync(IReadOnlyCollection<int> pedidoIds, CancellationToken ct)
    {
        if (pedidoIds.Count == 0)
        {
            return Array.Empty<BeneficioDto>().ToLookup(b => 0);
        }
        var list = await ToDtosAsync(db.Beneficios.Where(b => b.PedidoId != null && pedidoIds.Contains(b.PedidoId.Value) && b.Active), ct);
        return list.ToLookup(b => b.PedidoId!.Value);
    }

    /// <summary>
    /// Checks the beneficios chosen when asking for a service and returns them tracked, to reserve them once the pedido
    /// exists (<see cref="Reserve"/>). Someone else's beneficio answers as if it did not exist.
    /// </summary>
    public async Task<List<Beneficio>> CheckReserveAsync(int invitadoId, IReadOnlyList<int>? ids, IReadOnlyCollection<int> pedidoLineas,
        DateOnly recoleccion, CancellationToken ct)
    {
        if (ids is not { Count: > 0 })
        {
            return [];
        }
        var wanted = ids.Distinct().ToList();
        var beneficios = await db.Beneficios.Include(b => b.Promocion)
            .Where(b => wanted.Contains(b.Id) && b.InvitadoId == invitadoId && b.Active)
            .ToListAsync(ct);
        if (beneficios.Count != wanted.Count)
        {
            throw new NotFoundException("Algún beneficio no existe o no es de este invitado.");
        }
        var lineas = await LineaNamesAsync(ct);
        var others = new List<Promocion>();
        foreach (var b in beneficios.OrderBy(b => wanted.IndexOf(b.Id)))
        {
            var p = b.Promocion!;
            BeneficioRules.EnsureCanReserve(b, p, p.LineaServicioId is { } l ? lineas.GetValueOrDefault(l) : null, clock.Today, recoleccion, pedidoLineas, others);
            others.Add(p);
        }
        return beneficios;
    }

    public static void Reserve(IEnumerable<Beneficio> beneficios, int pedidoId)
    {
        foreach (var b in beneficios)
        {
            b.PedidoId = pedidoId;
        }
    }

    /// <summary>A cancelled pedido frees the beneficios reserved for it (not the used ones).</summary>
    public async Task ReleaseAsync(int pedidoId, CancellationToken ct)
    {
        foreach (var b in await db.Beneficios.Where(b => b.PedidoId == pedidoId && b.UsedAt == null && b.Active).ToListAsync(ct))
        {
            b.PedidoId = null;
        }
    }

    /// <summary>
    /// The recolección of <paramref name="pedido"/> closed (chofer, or the office moving it on): its reserved beneficios
    /// are used, and the invitado gets the cuponera (first service) or the sellos it triggers (next ones). Only once per
    /// pedido. Returns the names granted. The caller saves.
    /// </summary>
    public async Task<IReadOnlyList<string>> OnRecolectadoAsync(Pedido pedido, DateTimeOffset at, CancellationToken ct)
    {
        foreach (var b in await db.Beneficios.Where(b => b.PedidoId == pedido.Id && b.UsedAt == null && b.Active).ToListAsync(ct))
        {
            b.UsedAt = at;
        }
        if (await db.Beneficios.AnyAsync(b => b.GrantedByPedidoId == pedido.Id && b.Origin == BeneficioOrigin.Automatico, ct)
            || !BeneficioRules.TakesPart(await db.Invitados.Where(i => i.Id == pedido.InvitadoId).Select(i => i.Type).FirstAsync(ct)))
        {
            return [];
        }

        var today = clock.Today;
        var promociones = await db.Promociones.Where(p => p.Active).ToListAsync(ct);
        var earlierService = await db.Pedidos.AnyAsync(p => p.InvitadoId == pedido.InvitadoId && p.Id != pedido.Id && p.Active
            && p.Etapa != PedidoEtapa.Agendado && p.Etapa != PedidoEtapa.Cancelado, ct);
        IReadOnlyList<Promocion> grant;
        if (!earlierService)
        {
            grant = BeneficioRules.WelcomeCoupons(promociones);
        }
        else
        {
            var lineas = await db.Pedidos.Where(p => p.Id == pedido.Id).SelectMany(p => p.LineasServicio.Select(l => l.LineaServicioId)).ToListAsync(ct);
            var held = (await db.Beneficios
                .Where(b => b.InvitadoId == pedido.InvitadoId && b.Active && b.UsedAt == null && (b.ExpiresOn == null || b.ExpiresOn >= today))
                .Select(b => b.PromocionId).ToListAsync(ct)).ToHashSet();
            grant = BeneficioRules.SellosFor(promociones, lineas, held);
        }
        foreach (var p in grant)
        {
            await AddAsync(pedido.InvitadoId, p, BeneficioOrigin.Automatico, pedido.Id, pedido.EntregaDate, ct);
        }
        return grant.Select(p => p.Name).ToList();
    }

    /// <summary>
    /// The entrega of <paramref name="pedidoId"/> was programmed or moved: the beneficios it gave that last "hasta la
    /// entrega del pedido" now expire that day. The caller saves.
    /// </summary>
    public async Task OnEntregaProgramadaAsync(int pedidoId, DateOnly entrega, CancellationToken ct)
    {
        foreach (var b in await db.Beneficios
                     .Where(b => b.GrantedByPedidoId == pedidoId && b.Active && b.UsedAt == null && b.Promocion!.ValidityDays == null)
                     .ToListAsync(ct))
        {
            b.ExpiresOn = entrega;
        }
    }

    /// <summary>
    /// A new beneficio with a unique code; the caller saves. <paramref name="entrega"/> is the entrega date of the pedido
    /// that gives it, for the ones that last until then.
    /// </summary>
    public async Task<Beneficio> AddAsync(int invitadoId, Promocion p, BeneficioOrigin origin, int? grantedBy, DateOnly? entrega,
        CancellationToken ct)
    {
        var beneficio = new Beneficio
        {
            PromocionId = p.Id,
            Promocion = p,
            InvitadoId = invitadoId,
            Code = await NewCodeAsync(ct),
            Origin = origin,
            GrantedByPedidoId = grantedBy,
            ExpiresOn = BeneficioRules.ExpiresOn(clock.Today, p.ValidityDays, entrega),
        };
        db.Beneficios.Add(beneficio);
        return beneficio;
    }

    public async Task<BeneficioDto> ToDtoAsync(Beneficio b, CancellationToken ct) =>
        ToDto(b, await LineaNamesAsync(ct), clock.Today);

    private async Task<string> NewCodeAsync(CancellationToken ct)
    {
        // Random and unguessable; codes not saved yet in this request count too.
        var pending = db.Beneficios.Local.Select(b => b.Code).ToHashSet();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = BeneficioRules.NewCode(RandomNumberGenerator.GetInt32);
            if (!pending.Contains(code) && !await db.Beneficios.AnyAsync(b => b.Code == code, ct))
            {
                return code;
            }
        }
        throw new InvalidOperationException("Could not find a free beneficio code.");
    }

    private async Task<List<BeneficioDto>> ToDtosAsync(IQueryable<Beneficio> query, CancellationToken ct)
    {
        var rows = await query.AsNoTracking().Include(b => b.Promocion)
            .OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id).ToListAsync(ct);
        var lineas = await LineaNamesAsync(ct);
        var today = clock.Today;
        return rows.Select(b => ToDto(b, lineas, today)).ToList();
    }

    private Task<Dictionary<int, string>> LineaNamesAsync(CancellationToken ct) =>
        db.LineasServicio.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Name, ct);

    private static BeneficioDto ToDto(Beneficio b, IReadOnlyDictionary<int, string> lineas, DateOnly today)
    {
        var p = b.Promocion!;
        var linea = p.LineaServicioId is { } id ? lineas.GetValueOrDefault(id) : null;
        return new BeneficioDto(b.Id, b.Code, p.Id, p.Name, p.Programa, BeneficioRules.Mechanic(p, linea), p.Condition, p.LineaServicioId, linea,
            p.Stackable, BeneficioRules.State(b, today), b.Origin, b.CreatedAt, b.ExpiresOn, b.PedidoId, b.UsedAt);
    }
}

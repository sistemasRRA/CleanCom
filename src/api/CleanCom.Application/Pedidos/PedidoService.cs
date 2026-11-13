using CleanCom.Application.Common;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Pedidos;

/// <summary>
/// Scheduled pedidos and their removal. The Pedidos board (stages, history) arrives in week 4.
/// </summary>
public class PedidoService(ICleanComDbContext db, IBusinessClock clock)
{
    // A month of the agenda board fits: 14 cupo x rutas x working days.
    private const int ListLimit = 1000;

    /// <summary>
    /// Pedidos by recolección date. Without filters, from today on; with an invitado, all of theirs.
    /// </summary>
    public async Task<IReadOnlyList<PedidoDto>> ListAsync(DateOnly? from, DateOnly? to, int? invitadoId, CancellationToken ct)
    {
        if (from is null && to is null && invitadoId is null)
        {
            from = clock.Today;
        }

        var rows = await db.Pedidos.AsNoTracking()
            .Where(p => p.Active
                && (from == null || p.RecoleccionDate >= from)
                && (to == null || p.RecoleccionDate <= to)
                && (invitadoId == null || p.InvitadoId == invitadoId))
            .OrderBy(p => p.RecoleccionDate).ThenBy(p => p.Id)
            .Take(ListLimit)
            .Select(p => new
            {
                p.Id,
                p.InvitadoId,
                InvitadoName = p.Invitado!.Name,
                p.Invitado.WhatsAppPhone,
                p.DireccionId,
                p.Direccion!.Street,
                p.Direccion.ExteriorNumber,
                p.Direccion.InteriorNumber,
                p.Direccion.Neighborhood,
                ZonaCode = p.Direccion.Zona == null ? null : p.Direccion.Zona.Code,
                RutaCode = db.Paradas.Where(x => x.PedidoId == p.Id && x.Active).Select(x => x.RutaDia!.Ruta!.Code).FirstOrDefault(),
                p.Etapa,
                p.Turno,
                p.SpecificTime,
                p.Priority,
                p.GarmentsApprox,
                p.DriverNotes,
                p.RegisteredAt,
                p.Origin,
                p.RecoleccionDate,
                Lineas = p.LineasServicio.Select(l => l.LineaServicio!.Name).ToList(),
            })
            .ToListAsync(ct);

        return rows.Select(p => new PedidoDto(
            p.Id, p.InvitadoId, p.InvitadoName, p.WhatsAppPhone, p.DireccionId,
            Address(p.Street, p.ExteriorNumber, p.InteriorNumber, p.Neighborhood),
            p.ZonaCode, p.RutaCode, p.Etapa, p.Turno, p.SpecificTime, p.Priority, p.GarmentsApprox, p.DriverNotes,
            p.RegisteredAt, p.Origin, p.RecoleccionDate, p.Lineas)).ToList();
    }

    /// <summary>
    /// Baja lógica of the pedido and its paradas: it leaves the lists and frees its place in the agenda.
    /// Nothing is deleted from the database. Administrators only (checked by the endpoint).
    /// </summary>
    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        if (!await db.Pedidos.AnyAsync(p => p.Id == id && p.Active, ct))
        {
            throw new NotFoundException($"El pedido {id} no existe o ya fue eliminado.");
        }
        await MarkInactiveAsync([id], ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Marks the pedidos and their paradas as inactive (saved by the caller). Also used when an invitado is removed.
    /// </summary>
    public async Task MarkInactiveAsync(IReadOnlyCollection<int> pedidoIds, CancellationToken ct)
    {
        if (pedidoIds.Count == 0)
        {
            return;
        }
        foreach (var parada in await db.Paradas.Where(p => p.PedidoId != null && pedidoIds.Contains(p.PedidoId.Value) && p.Active).ToListAsync(ct))
        {
            parada.Active = false;
        }
        foreach (var pedido in await db.Pedidos.Where(p => pedidoIds.Contains(p.Id) && p.Active).ToListAsync(ct))
        {
            pedido.Active = false;
        }
    }

    private static string Address(string street, string? exterior, string? interior, string? neighborhood)
    {
        var line = street + (exterior is null ? "" : $" {exterior}") + (interior is null ? "" : $" int. {interior}");
        return neighborhood is null ? line : $"{line}, {neighborhood}";
    }
}

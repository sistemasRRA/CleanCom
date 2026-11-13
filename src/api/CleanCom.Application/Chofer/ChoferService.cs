using CleanCom.Application.Common;
using CleanCom.Application.Operations;
using CleanCom.Domain.Agenda;
using CleanCom.Domain.Operations;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Chofer;

/// <summary>
/// What the chofer app shows (docs/pantallas/apps/chofer.md). The chofer is the operador linked to the signed-in user.
/// </summary>
public class ChoferService(ICleanComDbContext db, ICurrentUser currentUser, IBusinessClock clock, ISettingService settings)
{
    /// <summary>
    /// Paradas of the chofer's ruta on a day (today in Mexico City when empty). The ruta is the one the office assigned
    /// to this operador that day; otherwise their usual ruta, unless the office gave that day to another operador.
    /// </summary>
    public async Task<ChoferRutaDto> RutaAsync(DateOnly? date, CancellationToken ct)
    {
        var day = date ?? clock.Today;
        var label = AgendaCalculator.Format(day);

        var userId = currentUser.Id ?? throw new UnauthorizedException("La sesión no es válida.");
        var operador = await db.Operadores.AsNoTracking().FirstOrDefaultAsync(o => o.UserId == userId && o.Active, ct);
        if (operador is null)
        {
            return Empty("Tu usuario no está ligado a un operador. Pide a oficina que lo ligue en Usuarios.");
        }

        var rutaDia = await db.RutaDias.AsNoTracking().Include(d => d.Ruta)
            .FirstOrDefaultAsync(d => d.Date == day && d.OperadorId == operador.Id, ct);
        var assigned = rutaDia is not null;
        if (rutaDia is null && operador.UsualRutaId is not null)
        {
            rutaDia = await db.RutaDias.AsNoTracking().Include(d => d.Ruta)
                .FirstOrDefaultAsync(d => d.Date == day && d.RutaId == operador.UsualRutaId, ct);
            if (rutaDia?.OperadorId is not null)
            {
                return Empty("Oficina asignó tu ruta habitual a otro chofer este día.", operador.Name);
            }
        }

        var ruta = rutaDia?.Ruta ?? (operador.UsualRutaId is null ? null
            : await db.Rutas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == operador.UsualRutaId, ct));
        if (ruta is null)
        {
            return Empty("No tienes ruta asignada. Pide a oficina que te asigne una ruta habitual.", operador.Name);
        }
        var rutaInfo = new ChoferRutaInfoDto(ruta.Id, ruta.Code, ruta.Name, ruta.Color);

        var nonWorking = await db.NonWorkingDays.AsNoTracking()
            .Where(d => d.Active && d.Date == day).Select(d => d.Reason).FirstOrDefaultAsync(ct);
        if (rutaDia is null)
        {
            return new ChoferRutaDto(day, label, operador.Name, rutaInfo, false,
                nonWorking is null ? $"La ruta {ruta.Code} no tiene paradas el {label}." : $"Día inhábil: {nonWorking}.", []);
        }

        var rows = await db.Paradas.AsNoTracking()
            .Where(p => p.RutaDiaId == rutaDia.Id && p.Active)
            .Select(p => new
            {
                p.Id,
                p.Position,
                p.Kind,
                p.State,
                p.Turno,
                p.Notes,
                p.PedidoId,
                SpecificTime = p.Pedido == null ? null : p.Pedido.SpecificTime,
                Priority = p.Pedido != null && p.Pedido.Priority,
                GarmentsApprox = p.Pedido == null ? null : p.Pedido.GarmentsApprox,
                InvitadoName = p.Pedido == null ? null : p.Pedido.Invitado!.Name,
                Phone = p.Pedido == null ? null : p.Pedido.Invitado!.WhatsAppPhone,
                Lineas = p.Pedido == null ? new List<string>() : p.Pedido.LineasServicio.Select(l => l.LineaServicio!.Name).ToList(),
                Street = p.Direccion == null ? null : p.Direccion.Street,
                Exterior = p.Direccion == null ? null : p.Direccion.ExteriorNumber,
                Interior = p.Direccion == null ? null : p.Direccion.InteriorNumber,
                Neighborhood = p.Direccion == null ? null : p.Direccion.Neighborhood,
                References = p.Direccion == null ? null : p.Direccion.References,
                ZonaCode = p.Direccion == null || p.Direccion.Zona == null ? null : p.Direccion.Zona.Code,
                Location = p.Direccion == null ? null : p.Direccion.Location,
                LugarName = p.Lugar == null ? null : p.Lugar.Name,
                LugarAddress = p.Lugar == null ? null : p.Lugar.Address,
            })
            .ToListAsync(ct);

        var morningEnd = (await settings.GetTimeRangeAsync(SettingKeys.TurnoMorning, ct)).End;
        var paradas = rows
            .OrderBy(p => p.Position == 0 ? int.MaxValue : p.Position)
            .ThenBy(p => ProvisionalOrder.Rank(p.Turno, p.SpecificTime, morningEnd))
            .ThenBy(p => p.SpecificTime ?? TimeOnly.MaxValue)
            .ThenByDescending(p => p.Priority)
            .ThenBy(p => p.Id)
            .Select((p, i) => new ChoferParadaDto(
                p.Id, i + 1, p.Kind, p.State, p.Turno, p.SpecificTime, p.Priority, p.PedidoId,
                p.InvitadoName ?? p.LugarName, p.Phone,
                p.Street is null ? p.LugarAddress : Address(p.Street, p.Exterior, p.Interior, p.Neighborhood),
                p.References, p.ZonaCode, p.Location?.Y, p.Location?.X, p.GarmentsApprox, p.Lineas, p.Notes))
            .ToList();

        var notice = nonWorking is not null ? $"Día inhábil: {nonWorking}." : paradas.Count == 0 ? $"La ruta {ruta.Code} no tiene paradas el {label}." : null;
        return new ChoferRutaDto(day, label, operador.Name, rutaInfo, assigned, notice, paradas);

        ChoferRutaDto Empty(string message, string? name = null) => new(day, label, name, null, false, message, []);
    }

    private static string Address(string street, string? exterior, string? interior, string? neighborhood)
    {
        var line = street + (exterior is null ? "" : $" {exterior}") + (interior is null ? "" : $" int. {interior}");
        return neighborhood is null ? line : $"{line}, {neighborhood}";
    }
}

using CleanCom.Application.Agenda;
using CleanCom.Application.Common;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Common;
using CleanCom.Domain.Geography;
using CleanCom.Domain.People;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Invitados;

/// <summary>
/// Invitados and their addresses: what Agendar needs (search, capture and update the ficha).
/// CRM status, contacts and the "Llamar hoy" list arrive with the CRM screen.
/// </summary>
public class InvitadoService(
    ICleanComDbContext db,
    ZonaLocator locator,
    PedidoService pedidos,
    IValidator<SaveInvitadoRequest> invitadoValidator,
    IValidator<SaveDireccionRequest> direccionValidator)
{
    private const int SearchLimit = 20;

    /// <summary>By name or phone (any format: "55 1234 5678" finds 5512345678).</summary>
    public async Task<IReadOnlyList<InvitadoDto>> SearchAsync(string? q, CancellationToken ct)
    {
        var text = q?.Trim() ?? "";
        if (text.Length < 2)
        {
            return [];
        }
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());

        var invitados = await Query()
            .Where(i => i.Active && (i.Name.Contains(text)
                || (digits.Length >= 4 && (i.WhatsAppPhone.Contains(digits) || (i.OtherPhone != null && i.OtherPhone.Contains(digits))))))
            .OrderBy(i => i.Name)
            .Take(SearchLimit)
            .ToListAsync(ct);
        return invitados.Select(ToDto).ToList();
    }

    public async Task<InvitadoDto> GetAsync(int id, CancellationToken ct) =>
        ToDto(await Query().FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new NotFoundException($"El invitado {id} no existe."));

    public async Task<InvitadoDto> CreateAsync(SaveInvitadoRequest r, CancellationToken ct)
    {
        await invitadoValidator.ValidateAndThrowAsync(r, ct);
        var invitado = new Invitado();
        await ApplyAsync(invitado, r, ct);
        if (r.Direccion is not null)
        {
            var direccion = new Direccion();
            await ApplyAsync(direccion, r.Direccion, ct);
            invitado.Direcciones.Add(direccion);
        }
        db.Invitados.Add(invitado);
        await db.SaveChangesAsync(ct);
        return await GetAsync(invitado.Id, ct);
    }

    public async Task<InvitadoDto> UpdateAsync(int id, SaveInvitadoRequest r, CancellationToken ct)
    {
        await invitadoValidator.ValidateAndThrowAsync(r, ct);
        var invitado = await db.Invitados.FirstOrDefaultAsync(i => i.Id == id && i.Active, ct) ?? throw new NotFoundException($"El invitado {id} no existe.");
        await ApplyAsync(invitado, r, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<InvitadoDto> AddDireccionAsync(int invitadoId, SaveDireccionRequest r, CancellationToken ct)
    {
        await direccionValidator.ValidateAndThrowAsync(r, ct);
        if (!await db.Invitados.AnyAsync(i => i.Id == invitadoId && i.Active, ct))
        {
            throw new NotFoundException($"El invitado {invitadoId} no existe.");
        }
        var direccion = new Direccion { InvitadoId = invitadoId };
        await ApplyAsync(direccion, r, ct);
        db.Direcciones.Add(direccion);
        await db.SaveChangesAsync(ct);
        return await GetAsync(invitadoId, ct);
    }

    public async Task<InvitadoDto> UpdateDireccionAsync(int invitadoId, int direccionId, SaveDireccionRequest r, CancellationToken ct)
    {
        await direccionValidator.ValidateAndThrowAsync(r, ct);
        var direccion = await db.Direcciones.FirstOrDefaultAsync(d => d.Id == direccionId && d.InvitadoId == invitadoId, ct)
            ?? throw new NotFoundException("La dirección no existe o no es de este invitado.");
        await ApplyAsync(direccion, r, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(invitadoId, ct);
    }

    public async Task DeactivateDireccionAsync(int invitadoId, int direccionId, CancellationToken ct)
    {
        var direccion = await db.Direcciones.FirstOrDefaultAsync(d => d.Id == direccionId && d.InvitadoId == invitadoId, ct)
            ?? throw new NotFoundException("La dirección no existe o no es de este invitado.");
        direccion.Active = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Baja lógica of the invitado with their addresses and every pedido (with its paradas, which free their cupo).
    /// Nothing is deleted from the database. Administrators only (checked by the endpoint).
    /// </summary>
    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        var invitado = await db.Invitados.Include(i => i.Direcciones).FirstOrDefaultAsync(i => i.Id == id && i.Active, ct)
            ?? throw new NotFoundException($"El invitado {id} no existe o ya fue eliminado.");
        if (invitado.UserId is not null)
        {
            throw new DomainException($"{invitado.Name} tiene cuenta en la app; bloquea su cuenta antes de eliminarlo.");
        }

        var pedidoIds = await db.Pedidos.Where(p => p.InvitadoId == id && p.Active).Select(p => p.Id).ToListAsync(ct);
        await pedidos.MarkInactiveAsync(pedidoIds, ct);
        invitado.Direcciones.ForEach(d => d.Active = false);
        invitado.Active = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyAsync(Invitado invitado, SaveInvitadoRequest r, CancellationToken ct)
    {
        var phone = Phones.Normalize(r.WhatsAppPhone);
        // Only active invitados: a WhatsApp given de baja can be captured again.
        var duplicate = await db.Invitados.AsNoTracking()
            .Where(i => i.WhatsAppPhone == phone && i.Id != invitado.Id && i.Active)
            .Select(i => i.Name)
            .FirstOrDefaultAsync(ct);
        if (duplicate is not null)
        {
            throw new ConflictException($"El WhatsApp {phone} ya es de {duplicate}. Búscalo y usa su ficha.");
        }

        invitado.Name = r.Name.Trim();
        invitado.WhatsAppPhone = phone;
        invitado.OtherPhone = Clean(r.OtherPhone);
        invitado.Email = Clean(r.Email);
        invitado.Type = r.Type;
        invitado.HowFoundUs = Clean(r.HowFoundUs);
        invitado.Campaign = Clean(r.Campaign);
        invitado.Notes = Clean(r.Notes);
    }

    /// <summary>Saves the address and its zona: the one chosen by hand, or the one found by polygon or postal code.</summary>
    private async Task ApplyAsync(Direccion d, SaveDireccionRequest r, CancellationToken ct)
    {
        d.Label = Clean(r.Label);
        d.Street = r.Street.Trim();
        d.ExteriorNumber = Clean(r.ExteriorNumber);
        d.InteriorNumber = Clean(r.InteriorNumber);
        d.PostalCode = r.PostalCode.Trim();
        d.Neighborhood = Clean(r.Neighborhood);
        d.Borough = Clean(r.Borough);
        d.References = Clean(r.References);
        d.Location = r.Lat is not null && r.Lng is not null ? Geo.CreatePoint(r.Lat.Value, r.Lng.Value) : null;

        var location = r.ZonaId is not null
            ? await locator.ManualAsync(r.ZonaId.Value, ct)
            : await locator.LocateAsync(d.Location, d.PostalCode, d.Neighborhood, ct);
        d.ZonaId = location.Zona?.Id;
        d.ZonaAssignedManually = location.Source == ZonaSource.Manual;
    }

    private IQueryable<Invitado> Query() => db.Invitados.AsNoTracking()
        .Include(i => i.Direcciones.Where(d => d.Active))
        .ThenInclude(d => d.Zona);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static InvitadoDto ToDto(Invitado i) => new(
        i.Id, i.Name, i.WhatsAppPhone, i.OtherPhone, i.Email, i.Type, i.HowFoundUs, i.Campaign, i.Notes, i.Active,
        i.Direcciones.OrderBy(d => d.Id).Select(d => new DireccionDto(
            d.Id, d.Label, d.Street, d.ExteriorNumber, d.InteriorNumber, d.PostalCode, d.Neighborhood, d.Borough, d.References,
            d.Location?.Y, d.Location?.X, d.ZonaId, d.Zona?.Code, d.ZonaAssignedManually, d.Active)).ToList());
}

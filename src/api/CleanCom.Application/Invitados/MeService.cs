using CleanCom.Application.Common;
using CleanCom.Domain.People;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Application.Invitados;

/// <summary>
/// The signed-in invitado's own data and addresses (docs/pantallas/apps/invitado.md, Mis datos and Mis direcciones).
/// Same rules as the office (InvitadoService); the invitado only reaches their own record.
/// </summary>
public class MeService(ICleanComDbContext db, ICurrentUser currentUser, InvitadoService invitados)
{
    public async Task<MeDto> GetAsync(CancellationToken ct) => ToMe(await invitados.GetAsync(await OwnIdAsync(ct), ct));

    public async Task<MeDto> UpdateAsync(UpdateMeRequest r, CancellationToken ct)
    {
        var id = await OwnIdAsync(ct);
        var current = await db.Invitados.AsNoTracking().FirstAsync(i => i.Id == id, ct);
        // What only the office changes stays as it is: phone, type, campaign and notes.
        var dto = await invitados.UpdateAsync(id, new SaveInvitadoRequest(
            r.Name, current.WhatsAppPhone, r.OtherPhone, r.Email, current.Type, r.HowFoundUs, current.Campaign, current.Notes,
            null, r.ReceivesWhatsApp, r.ReceivesPush, r.ReceivesPromotions), ct);
        return ToMe(dto);
    }

    /// <summary>The zona is located from the address; only the office chooses one by hand.</summary>
    public async Task<MeDto> AddAddressAsync(SaveAddressRequest r, CancellationToken ct) =>
        ToMe(await invitados.AddAddressAsync(await OwnIdAsync(ct), r with { ZonaId = null }, ct));

    /// <summary>A zona the office chose by hand stays; otherwise it is located again.</summary>
    public async Task<MeDto> UpdateAddressAsync(int addressId, SaveAddressRequest r, CancellationToken ct)
    {
        var id = await OwnIdAsync(ct);
        var manualZona = await db.Addresses.AsNoTracking()
            .Where(a => a.Id == addressId && a.InvitadoId == id && a.ZonaAssignedManually)
            .Select(a => a.ZonaId).FirstOrDefaultAsync(ct);
        return ToMe(await invitados.UpdateAddressAsync(id, addressId, r with { ZonaId = manualZona }, ct));
    }

    public async Task<MeDto> DeactivateAddressAsync(int addressId, CancellationToken ct)
    {
        var id = await OwnIdAsync(ct);
        await invitados.DeactivateAddressAsync(id, addressId, ct);
        return ToMe(await invitados.GetAsync(id, ct));
    }

    /// <summary>The invitado linked to the signed-in user.</summary>
    internal async Task<int> OwnIdAsync(CancellationToken ct)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedException("La sesión no es válida.");
        return await db.Invitados.AsNoTracking().Where(i => i.UserId == userId && i.Active).Select(i => (int?)i.Id).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tu cuenta no está ligada a un invitado. Escríbenos por WhatsApp para ayudarte.");
    }

    private static MeDto ToMe(InvitadoDto i) => new(
        i.Id, i.Name, i.WhatsAppPhone, i.OtherPhone, i.Email, i.HowFoundUs, i.ReceivesWhatsApp, i.ReceivesPush, i.ReceivesPromotions,
        !string.IsNullOrWhiteSpace(i.HowFoundUs) && !IsPlaceholderName(i.Name, i.WhatsAppPhone),
        i.Addresses.Where(a => a.Active).ToList());

    /// <summary>The name given when the invitado signed up from the app, before they wrote theirs.</summary>
    internal static bool IsPlaceholderName(string name, string phone) => name == $"Invitado {phone[^4..]}";
}

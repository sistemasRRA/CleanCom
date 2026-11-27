using CleanCom.Domain.Common;
using CleanCom.Domain.People;

namespace CleanCom.Tests.Crm;

/// <summary>App account and duplicates of an invitado (docs/03-reglas-de-negocio.md, CRM › Duplicados y cuenta de la app).</summary>
public class InvitadoAccountTests
{
    [Fact]
    public void The_account_state_follows_the_user_and_the_invitation()
    {
        Assert.Equal(AppAccountState.SinApp, InvitadoAccount.State(false, false, null));
        Assert.Equal(AppAccountState.Invitado, InvitadoAccount.State(false, false, DateTimeOffset.UtcNow));
        Assert.Equal(AppAccountState.Activa, InvitadoAccount.State(true, true, null));
        Assert.Equal(AppAccountState.Bloqueada, InvitadoAccount.State(true, false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Duplicates_share_a_phone_whatever_its_format()
    {
        var a = new Invitado { Id = 1, WhatsAppPhone = "5512345678" };
        var b = new Invitado { Id = 2, WhatsAppPhone = "5587654321", OtherPhone = "+52 1 55 1234 5678" };
        var c = new Invitado { Id = 3, WhatsAppPhone = "5500000000" };
        Assert.True(InvitadoAccount.SharePhone(a, b));
        Assert.False(InvitadoAccount.SharePhone(a, c));
    }

    [Fact]
    public void The_main_invitado_keeps_their_data_and_takes_what_is_missing()
    {
        // Example of the document.
        var main = new Invitado { Id = 1, Name = "Ana López", WhatsAppPhone = "5512345678", Notes = "Portón negro" };
        var duplicate = new Invitado { Id = 2, Name = "Ana", WhatsAppPhone = "5587654321", Email = "ana@correo.mx", HowFoundUs = "Google (búsqueda o Maps)", Notes = "Tocar dos veces", UserId = 40 };
        InvitadoAccount.TakeMissing(main, duplicate);
        Assert.Equal("5512345678", main.WhatsAppPhone);
        Assert.Equal("5587654321", main.OtherPhone);
        Assert.Equal("ana@correo.mx", main.Email);
        Assert.Equal("Google (búsqueda o Maps)", main.HowFoundUs);
        Assert.Equal("Portón negro\nTocar dos veces", main.Notes);
        Assert.Equal(40, main.UserId);
        Assert.Null(duplicate.UserId);
        Assert.False(duplicate.Active);
        Assert.Contains("Fusionado con el invitado 1", duplicate.Notes);
    }

    [Fact]
    public void Capturing_an_existing_whatsapp_blocks_and_any_other_shared_phone_warns()
    {
        // Example of the document: Ana has 5512345678 as WhatsApp, Luis has it as his other phone.
        var ana = new Invitado { Id = 1, WhatsAppPhone = "5512345678" };
        var luis = new Invitado { Id = 2, WhatsAppPhone = "5587654321", OtherPhone = "5512345678" };
        Assert.True(InvitadoAccount.CaptureConflict("55 1234 5678", null, ana));
        Assert.False(InvitadoAccount.CaptureConflict("5512345678", null, luis));
        Assert.False(InvitadoAccount.CaptureConflict("5500000000", "+52 1 55 1234 5678", ana));
        Assert.Null(InvitadoAccount.CaptureConflict("5500000000", null, ana));
        Assert.Null(InvitadoAccount.CaptureConflict("123", "", ana));
    }

    [Fact]
    public void Two_app_accounts_or_the_same_invitado_cannot_be_merged()
    {
        var main = new Invitado { Id = 1, WhatsAppPhone = "5512345678", UserId = 10 };
        var duplicate = new Invitado { Id = 2, WhatsAppPhone = "5587654321", UserId = 11 };
        Assert.Contains("cuenta en la app", Assert.Throws<DomainException>(() => InvitadoAccount.EnsureCanMerge(main, duplicate)).Message);
        Assert.Throws<DomainException>(() => InvitadoAccount.EnsureCanMerge(main, main));
        duplicate.UserId = null;
        InvitadoAccount.EnsureCanMerge(main, duplicate);
    }
}

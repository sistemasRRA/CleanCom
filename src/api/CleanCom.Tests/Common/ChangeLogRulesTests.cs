using CleanCom.Domain.Common;

namespace CleanCom.Tests.Common;

/// <summary>Bitácora de cambios (docs/03-reglas-de-negocio.md).</summary>
public class ChangeLogRulesTests
{
    [Fact]
    public void Only_the_fields_that_really_changed_are_logged()
    {
        // Example of the document: the office changes the colonia; the stamps of who and when are not fields.
        var changes = ChangeLogRules.Diff(
        [
            ("Neighborhood", "Portales", "Portales Norte"),
            ("Street", "Tlalpan", "Tlalpan"),
            ("ModifiedAt", null, DateTimeOffset.UtcNow),
            ("LastAccess", null, DateTimeOffset.UtcNow),
        ]);
        var change = Assert.Single(changes);
        Assert.Equal(new FieldChange("Neighborhood", "Portales", "Portales Norte"), change);
    }

    [Fact]
    public void Turning_active_off_is_a_baja_and_on_a_reactivacion()
    {
        Assert.Equal(ChangeAction.Baja, ChangeLogRules.ModifiedAction(ChangeLogRules.Diff([("Active", true, false)])));
        Assert.Equal(ChangeAction.Reactivacion, ChangeLogRules.ModifiedAction(ChangeLogRules.Diff([("Active", false, true)])));
        Assert.Equal(ChangeAction.Cambio, ChangeLogRules.ModifiedAction(ChangeLogRules.Diff([("Name", "Ana", "Ana López")])));
    }

    [Fact]
    public void Values_are_written_as_the_office_reads_them()
    {
        Assert.Equal("Sí", ChangeLogRules.Format(true));
        Assert.Equal("2026-11-27", ChangeLogRules.Format(new DateOnly(2026, 11, 27)));
        Assert.Equal("13:30", ChangeLogRules.Format(new TimeOnly(13, 30)));
        Assert.Equal("LeToca", ChangeLogRules.Format(CleanCom.Domain.Crm.CrmStatus.LeToca));
        Assert.Equal("1250.5", ChangeLogRules.Format(1250.5m));
        Assert.Null(ChangeLogRules.Format(null));
        var cut = ChangeLogRules.Format(new string('a', 400))!;
        Assert.Equal(ChangeLogRules.MaxValueLength, cut.Length);
        Assert.EndsWith("…", cut);
    }

    [Fact]
    public void Fields_are_named_in_spanish()
    {
        Assert.Equal("Colonia", ChangeLogLabels.Field("Address", "Neighborhood"));
        Assert.Equal("Usuario", ChangeLogLabels.Field("UserRole", "UserId"));
        Assert.Equal("Cuenta de la app", ChangeLogLabels.Field("Invitado", "UserId"));
        Assert.Equal("Dirección", ChangeLogLabels.Entity("Address"));
        Assert.Equal("CampoNuevo", ChangeLogLabels.Field("Invitado", "CampoNuevo"));
    }
}

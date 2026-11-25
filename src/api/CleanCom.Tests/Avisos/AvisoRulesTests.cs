using CleanCom.Application.Avisos;
using CleanCom.Domain.Avisos;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;

namespace CleanCom.Tests.Avisos;

/// <summary>Avisos al invitado (docs/03-reglas-de-negocio.md): canal, evento por etapa, plantillas y texto.</summary>
public class AvisoRulesTests
{
    [Fact]
    public void Push_if_it_has_the_app_otherwise_whatsapp_only_if_it_accepts_it()
    {
        Assert.Equal(AvisoCanal.Push, AvisoRules.Channel(devices: 1, receivesWhatsApp: false, "5512345678"));
        Assert.Equal(AvisoCanal.WhatsApp, AvisoRules.Channel(devices: 0, receivesWhatsApp: true, "5512345678"));
        // Without app and without accepting WhatsApp it is not sent (recorded as omitted, with the reason).
        Assert.Null(AvisoRules.Channel(devices: 0, receivesWhatsApp: false, "5512345678"));
        Assert.Null(AvisoRules.Channel(devices: 0, receivesWhatsApp: true, ""));
        Assert.Equal("No acepta avisos por WhatsApp y no tiene la app.", AvisoRules.OmittedReason(false));
    }

    [Fact]
    public void Tiendas_internas_are_not_told()
    {
        Assert.False(AvisoRules.IsNotified(InvitadoType.TiendaInterna));
        Assert.True(AvisoRules.IsNotified(InvitadoType.ClienteFinal));
        Assert.True(AvisoRules.IsNotified(InvitadoType.Empresa));
    }

    [Fact]
    public void Each_etapa_change_gives_its_aviso()
    {
        Assert.Equal(AvisoEvento.RecoleccionRealizada, AvisoRules.ForEtapa(PedidoEtapa.Recolectado));
        Assert.Equal(AvisoEvento.PrendasEnProceso, AvisoRules.ForEtapa(PedidoEtapa.EnPlanta));
        Assert.Equal(AvisoEvento.EntregaEnCamino, AvisoRules.ForEtapa(PedidoEtapa.EnCamino));
        Assert.Equal(AvisoEvento.Entregado, AvisoRules.ForEtapa(PedidoEtapa.Entregado));
        Assert.Equal(AvisoEvento.PedidoCancelado, AvisoRules.ForEtapa(PedidoEtapa.Cancelado));
        // Entrega confirmada carries the date: it is told when the entrega is placed, not by the etapa.
        Assert.Null(AvisoRules.ForEtapa(PedidoEtapa.EntregaConfirmada));
        Assert.Null(AvisoRules.ForEtapa(PedidoEtapa.Listo));
    }

    [Fact]
    public void The_template_is_filled_with_its_variables_in_order()
    {
        var text = AvisoRules.Render("Hola {{1}}, pasamos el {{2}}, {{3}}.",
            [AvisoRules.FirstName("Ana López"), AvisoRules.Date(new DateOnly(2026, 11, 26)), AvisoRules.Turno(Turno.Manana, null)]);
        Assert.Equal("Hola Ana, pasamos el jueves 26 de noviembre, en la mañana.", text);
        Assert.Equal("Hola .", AvisoRules.Render("Hola {{1}}.{{4}}", [""]));
        Assert.Equal([1, 3], AvisoRules.UsedVariables("{{3}} y {{1}} y {{1}}"));
        Assert.Equal("a las 09:30", AvisoRules.Turno(Turno.HoraEspecifica, new TimeOnly(9, 30)));
        Assert.Equal("12 prendas", AvisoRules.Garments(12));
        Assert.Equal("tus prendas", AvisoRules.Garments(null));
    }

    [Fact]
    public void Sending_by_hand_opens_whatsapp_with_the_text()
    {
        Assert.Equal("https://wa.me/525512345678?text=Hola%20Ana%2C%20%C2%A1listo%21", AvisoRules.WhatsAppLink("5512345678", "Hola Ana, ¡listo!"));
    }

    [Fact]
    public void There_is_one_template_per_event_with_the_current_meta_names()
    {
        var defaults = AvisoRules.Defaults();
        Assert.Equal(Enum.GetValues<AvisoEvento>().Order(), defaults.Select(p => p.Evento).Order());
        Assert.Equal("cleancom_cita_agendada", defaults.Single(p => p.Evento == AvisoEvento.RecoleccionAgendada).MetaName);
        Assert.Equal("cleancom_chofer_en_camino", defaults.Single(p => p.Evento == AvisoEvento.ChoferEnCamino).MetaName);
        Assert.Equal("cleancom_entrega_confirmada", defaults.Single(p => p.Evento == AvisoEvento.EntregaConfirmada).MetaName);
        // "Prendas en proceso" is inactive today, as in the current system.
        Assert.False(defaults.Single(p => p.Evento == AvisoEvento.PrendasEnProceso).Active);
        var validator = new PlantillaValidator();
        Assert.All(defaults, p => Assert.True(validator.Validate(new PlantillaInput(p.Evento,
            new SavePlantillaRequest(p.MetaName, p.WhatsAppText, p.PushTitle, p.PushText, p.Active))).IsValid, p.MetaName));
    }

    [Fact]
    public void A_template_cannot_use_a_variable_its_event_does_not_have()
    {
        var result = new PlantillaValidator().Validate(new PlantillaInput(AvisoEvento.Entregado,
            new SavePlantillaRequest("cleancom_entregado", "Hola {{1}}, el {{2}}", "Entregado", "Listo", true)));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("{{2}} no existe"));
        Assert.False(new PlantillaValidator().Validate(new PlantillaInput(AvisoEvento.Entregado,
            new SavePlantillaRequest("Cleancom Entregado", "Hola", "Entregado", "Listo", true))).IsValid);
    }
}

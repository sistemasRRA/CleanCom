using System.Text.RegularExpressions;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Avisos;

/// <summary>
/// Avisos al invitado (docs/03-reglas-de-negocio.md): which event a change gives, by which channel it goes, the
/// variables of each template and how its text is filled in.
/// </summary>
public static partial class AvisoRules
{
    /// <summary>Separates the saved values of the variables (it cannot appear in a value).</summary>
    public const char ValueSeparator = '\u001F';

    /// <summary>The variables {{1}}, {{2}}… of each event, in order, as the office reads them.</summary>
    public static IReadOnlyList<string> Variables(AvisoEvento evento) => evento switch
    {
        AvisoEvento.RecoleccionAgendada or AvisoEvento.CambioDeDia or AvisoEvento.EntregaConfirmada => ["nombre", "fecha", "turno"],
        AvisoEvento.PedidoCancelado => ["nombre", "número de pedido"],
        AvisoEvento.RecoleccionRealizada => ["nombre", "prendas"],
        _ => ["nombre"],
    };

    /// <summary>Spanish name of the event.</summary>
    public static string Name(AvisoEvento evento) => evento switch
    {
        AvisoEvento.RecoleccionAgendada => "Recolección agendada",
        AvisoEvento.CambioDeDia => "Cambio de día",
        AvisoEvento.PedidoCancelado => "Pedido cancelado",
        AvisoEvento.ChoferEnCamino => "Chofer en camino",
        AvisoEvento.RecoleccionRealizada => "Recolección realizada",
        AvisoEvento.PrendasEnProceso => "Prendas en proceso",
        AvisoEvento.EntregaConfirmada => "Entrega confirmada",
        AvisoEvento.EntregaEnCamino => "Entrega en camino",
        _ => "Entregado",
    };

    /// <summary>The event a change of etapa gives; null when the etapa has no notice of its own.</summary>
    /// <remarks>EntregaConfirmada is told when the entrega date is placed (it carries the date), not here.</remarks>
    public static AvisoEvento? ForEtapa(PedidoEtapa to) => to switch
    {
        PedidoEtapa.Recolectado => AvisoEvento.RecoleccionRealizada,
        PedidoEtapa.EnPlanta => AvisoEvento.PrendasEnProceso,
        PedidoEtapa.EnCamino => AvisoEvento.EntregaEnCamino,
        PedidoEtapa.Entregado => AvisoEvento.Entregado,
        PedidoEtapa.Cancelado => AvisoEvento.PedidoCancelado,
        _ => null,
    };

    /// <summary>Tiendas (uso interno) are not told: they are CleanCom itself.</summary>
    public static bool IsNotified(InvitadoType type) => type != InvitadoType.TiendaInterna;

    /// <summary>
    /// "Push si tiene app; si no, WhatsApp de utilidad": push when the invitado has a device registered and accepts
    /// push; otherwise WhatsApp only if it accepts service notices by WhatsApp. Null = no channel (the notice is
    /// recorded as omitted). Example: with the app but push turned off and WhatsApp accepted → WhatsApp.
    /// </summary>
    public static AvisoCanal? Channel(int devices, bool receivesPush, bool receivesWhatsApp, string? whatsAppPhone) =>
        devices > 0 && receivesPush ? AvisoCanal.Push
        : receivesWhatsApp && !string.IsNullOrWhiteSpace(whatsAppPhone) ? AvisoCanal.WhatsApp
        : null;

    /// <summary>Why a notice has no channel, for the history.</summary>
    public static string OmittedReason(int devices, bool receivesPush, bool receivesWhatsApp) =>
        receivesWhatsApp ? "Sin app ni número de WhatsApp."
        : devices > 0 && !receivesPush ? "Apagó los avisos push y por WhatsApp."
        : "No acepta avisos por WhatsApp y no tiene la app.";

    /// <summary>Fills {{1}}, {{2}}… with the values in order; a variable without value stays empty.</summary>
    public static string Render(string template, IReadOnlyList<string> values) =>
        VariablePattern().Replace(template, m =>
        {
            var index = int.Parse(m.Groups[1].Value) - 1;
            return index >= 0 && index < values.Count ? values[index] : "";
        }).Trim();

    /// <summary>The variable numbers a text uses ({{3}} → 3).</summary>
    public static IReadOnlyList<int> UsedVariables(string text) =>
        VariablePattern().Matches(text).Select(m => int.Parse(m.Groups[1].Value)).Distinct().Order().ToList();

    /// <summary>Opens WhatsApp (web or app) on the number with the text written, for sending it by hand.</summary>
    public static string WhatsAppLink(string phone, string text) =>
        $"https://wa.me/52{phone}?text={Uri.EscapeDataString(text)}";

    public static string Turno(Turno turno, TimeOnly? specificTime) => turno switch
    {
        Pedidos.Turno.Manana => "en la mañana",
        Pedidos.Turno.Tarde => "en la tarde",
        Pedidos.Turno.HoraEspecifica when specificTime is { } time => $"a las {time:HH\\:mm}",
        _ => "a cualquier hora",
    };

    /// <summary>A date as the notices write it: "jueves 26 de noviembre".</summary>
    public static string Date(DateOnly date) => date.ToString("dddd d 'de' MMMM", Spanish);

    private static readonly System.Globalization.CultureInfo Spanish = new("es-MX");

    /// <summary>The first name, as the notices greet ("Ana López" → "Ana").</summary>
    public static string FirstName(string name)
    {
        var trimmed = name.Trim();
        var space = trimmed.IndexOf(' ');
        return space > 0 ? trimmed[..space] : trimmed;
    }

    public static string Garments(int? garments) => garments switch
    {
        null or <= 0 => "tus prendas",
        1 => "1 prenda",
        _ => $"{garments} prendas",
    };

    /// <summary>The templates the system starts with: the four of the current system and the missing events.</summary>
    public static IReadOnlyList<Plantilla> Defaults() =>
    [
        New(AvisoEvento.RecoleccionAgendada, "cleancom_cita_agendada",
            "Hola {{1}}, agendamos la recolección de tus prendas para el {{2}}, {{3}}. Te avisaremos cuando el chofer vaya en camino. CleanCom",
            "Recolección agendada", "Pasamos por tus prendas el {{2}}, {{3}}."),
        New(AvisoEvento.CambioDeDia, "cleancom_cambio_de_dia",
            "Hola {{1}}, tu recolección quedó para el {{2}}, {{3}}. CleanCom",
            "Cambió tu recolección", "Ahora pasamos el {{2}}, {{3}}."),
        New(AvisoEvento.PedidoCancelado, "cleancom_pedido_cancelado",
            "Hola {{1}}, cancelamos tu pedido {{2}}. Cuando quieras, agenda de nuevo en la app o por este medio. CleanCom",
            "Pedido cancelado", "Cancelamos tu pedido {{2}}."),
        New(AvisoEvento.ChoferEnCamino, "cleancom_chofer_en_camino",
            "Hola {{1}}, tu chofer de CleanCom va en camino a recoger tus prendas.",
            "Tu chofer va en camino", "Va por tus prendas. Síguelo en la app."),
        New(AvisoEvento.RecoleccionRealizada, "cleancom_recoleccion_realizada",
            "Hola {{1}}, recogimos {{2}}. Te avisaremos cuando programemos la entrega. CleanCom",
            "Recogimos tus prendas", "Recogimos {{2}}."),
        New(AvisoEvento.PrendasEnProceso, "cleancom_prendas_en_proceso",
            "Hola {{1}}, tus prendas ya están en nuestra planta. CleanCom",
            "Tus prendas están en proceso", "Ya están en nuestra planta.", active: false),
        New(AvisoEvento.EntregaConfirmada, "cleancom_entrega_confirmada",
            "Hola {{1}}, entregamos tus prendas el {{2}}, {{3}}. CleanCom",
            "Entrega programada", "Te entregamos el {{2}}, {{3}}."),
        New(AvisoEvento.EntregaEnCamino, "cleancom_entrega_en_camino",
            "Hola {{1}}, tu chofer de CleanCom va en camino a entregarte tus prendas.",
            "Tu entrega va en camino", "Síguela en la app."),
        New(AvisoEvento.Entregado, "cleancom_entregado",
            "Hola {{1}}, entregamos tu pedido. ¡Gracias por confiar en CleanCom!",
            "Pedido entregado", "¡Gracias por confiar en CleanCom!"),
    ];

    private static Plantilla New(AvisoEvento evento, string metaName, string whatsApp, string pushTitle, string pushText, bool active = true) =>
        new() { Evento = evento, MetaName = metaName, WhatsAppText = whatsApp, PushTitle = pushTitle, PushText = pushText, Active = active };

    [GeneratedRegex(@"\{\{(\d+)\}\}")]
    private static partial Regex VariablePattern();
}

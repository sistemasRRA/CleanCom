using CleanCom.Domain.Common;

namespace CleanCom.Domain.Avisos;

/// <summary>What happened to a pedido that the invitado is told about (docs/03-reglas-de-negocio.md, Avisos al invitado).</summary>
public enum AvisoEvento
{
    RecoleccionAgendada,
    /// <summary>The recolección moved to another day (the invitado, the office, or not done by the chofer).</summary>
    CambioDeDia,
    PedidoCancelado,
    /// <summary>The chofer left for the recolección.</summary>
    ChoferEnCamino,
    RecoleccionRealizada,
    /// <summary>The clothes reached planta (optional; inactive today).</summary>
    PrendasEnProceso,
    /// <summary>The office programmed (or moved) the entrega.</summary>
    EntregaConfirmada,
    EntregaEnCamino,
    Entregado,
}

public enum AvisoCanal
{
    /// <summary>Expo Notifications to the invitado app.</summary>
    Push,
    /// <summary>A utility template of the WhatsApp Cloud API.</summary>
    WhatsApp,
}

public enum AvisoState
{
    /// <summary>Waiting for the sender.</summary>
    Pendiente,
    Enviado,
    /// <summary>Test mode: it was not sent, only recorded.</summary>
    Simulado,
    /// <summary>WhatsApp is not connected (or sending failed): the office sends it with one click.</summary>
    ParaMandarAMano,
    MandadoAMano,
    Fallido,
    /// <summary>Not sent: the invitado accepts no channel (no app and no WhatsApp notices).</summary>
    Omitido,
}

/// <summary>
/// The text of each event (WhatsApp y metas › Plantillas): the Meta template name, its WhatsApp text with variables
/// {{1}}, {{2}}… (see <see cref="AvisoRules.Variables"/>) and the push title and text. One per event.
/// </summary>
public class Plantilla : Entity
{
    public AvisoEvento Evento { get; set; }
    /// <summary>Name of the approved template in Meta ("cleancom_cita_agendada").</summary>
    public string MetaName { get; set; } = "";
    public string WhatsAppText { get; set; } = "";
    public string PushTitle { get; set; } = "";
    public string PushText { get; set; } = "";
    /// <summary>An inactive template sends nothing for its event.</summary>
    public bool Active { get; set; } = true;
}

/// <summary>
/// A notice to an invitado about a pedido, with the text already filled in. It is the log (pedido history, WhatsApp y
/// metas) and the outbox: the sender picks the pending ones. Never deleted.
/// </summary>
public class Aviso : Entity
{
    public int InvitadoId { get; set; }
    public int? PedidoId { get; set; }
    public Pedidos.Pedido? Pedido { get; set; }
    public AvisoEvento Evento { get; set; }
    /// <summary>Null when it was omitted.</summary>
    public AvisoCanal? Canal { get; set; }
    /// <summary>WhatsApp number (10 digits) for WhatsApp; empty for push (it goes to every device of the invitado).</summary>
    public string? Phone { get; set; }
    public string MetaName { get; set; } = "";
    /// <summary>Values of the template variables, in order, separated by <see cref="AvisoRules.ValueSeparator"/>.</summary>
    public string Values { get; set; } = "";
    public string? Title { get; set; }
    public string Text { get; set; } = "";
    public AvisoState State { get; set; } = AvisoState.Pendiente;
    /// <summary>Why it was omitted or failed, or the provider's answer.</summary>
    public string? Detail { get; set; }
    public int Attempts { get; set; }
    /// <summary>Sent, simulated or marked as sent by hand.</summary>
    public DateTimeOffset? SentAt { get; set; }
}

/// <summary>A device of the invitado app that can receive push notices (Expo push token).</summary>
public class DispositivoPush : Entity, IActivatable
{
    public int InvitadoId { get; set; }
    /// <summary>"ExponentPushToken[…]".</summary>
    public string Token { get; set; } = "";
    /// <summary>ios, android or web.</summary>
    public string Platform { get; set; } = "";
    public DateTimeOffset LastSeenAt { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>What the invitado did in the app that the office should see (campana de oficina).</summary>
public enum AvisoOficinaKind
{
    NuevoPedido,
    CambioDeDia,
    Cancelacion,
}

/// <summary>A notice to the office (the bell in the web), attended once by anyone. Never deleted.</summary>
public class AvisoOficina : Entity
{
    public AvisoOficinaKind Kind { get; set; }
    public int InvitadoId { get; set; }
    public int? PedidoId { get; set; }
    public string Text { get; set; } = "";
    public DateTimeOffset? AttendedAt { get; set; }
    public int? AttendedBy { get; set; }
}

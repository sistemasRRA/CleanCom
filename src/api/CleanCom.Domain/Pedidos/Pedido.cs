using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Pedidos;

/// <summary>docs/03-reglas-de-negocio.md, Pedidos › Etapas.</summary>
public enum PedidoEtapa
{
    Agendado,
    Recolectado,
    EnPlanta,
    Listo,
    EntregaConfirmada,
    EnCamino,
    Entregado,
    Cancelado,
}

/// <summary>When the invitado prefers the visit.</summary>
public enum Turno
{
    CualquierHora,
    Manana,
    Tarde,
    HoraEspecifica,
}

public enum PedidoOrigin
{
    Oficina,
    App,
}

/// <summary>
/// A service from recolección to entrega. Never deleted: removing it is a baja lógica.
/// </summary>
public class Pedido : Entity, IActivatable
{
    public bool Active { get; set; } = true;
    public int InvitadoId { get; set; }
    public Invitado? Invitado { get; set; }
    public int DireccionId { get; set; }
    public Direccion? Direccion { get; set; }
    public PedidoEtapa Etapa { get; set; } = PedidoEtapa.Agendado;
    public int? GarmentsApprox { get; set; }
    public Turno Turno { get; set; } = Turno.CualquierHora;
    /// <summary>Only the office can set it (with <see cref="Turno.HoraEspecifica"/>).</summary>
    public TimeOnly? SpecificTime { get; set; }
    /// <summary>Only the office can set it.</summary>
    public bool Priority { get; set; }
    public string? DriverNotes { get; set; }
    /// <summary>Hora de registro (UTC). Automatic in the app, editable by the office.</summary>
    public DateTimeOffset RegisteredAt { get; set; }
    public PedidoOrigin Origin { get; set; }
    public DateOnly RecoleccionDate { get; set; }
    public DateOnly? EntregaDate { get; set; }

    public List<PedidoLineaServicio> LineasServicio { get; set; } = [];
}

public class PedidoLineaServicio
{
    public int PedidoId { get; set; }
    public int LineaServicioId { get; set; }
    public LineaServicio? LineaServicio { get; set; }
}

using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Routing;

/// <summary>
/// What the chofer does at a parada ("qué se hace"). Who it is for is <see cref="ParadaCategory"/>.
/// Convenio, Insumos and Traspaso remain for paradas created before the category existed.
/// </summary>
public enum ParadaKind
{
    Recoleccion,
    Entrega,
    RecoleccionYEntrega,
    Convenio,
    Insumos,
    Traspaso,
    Salida,
    Regreso,
    Comida,
    /// <summary>Moving load between lugares (traspasos, insumos).</summary>
    Traslado,
}

/// <summary>Tipo de parada: who the visit is for.</summary>
public enum ParadaCategory
{
    ClienteFinal,
    /// <summary>Empresa o cliente con convenio y visita fija.</summary>
    Convenio,
    Insumos,
    Traspaso,
}

public enum ParadaState
{
    Pendiente,
    EnCamino,
    EnSitio,
    Realizada,
    NoRealizada,
}

/// <summary>
/// A stop of a <see cref="RutaDia"/>. Position, estimated time and the chofer's visit data arrive with routing
/// and the chofer app.
/// </summary>
public class Parada : Entity, IActivatable
{
    /// <summary>An inactive parada (its pedido was removed) does not take cupo.</summary>
    public bool Active { get; set; } = true;
    public int RutaDiaId { get; set; }
    public RutaDia? RutaDia { get; set; }
    public ParadaKind Kind { get; set; }
    public int? PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public int? AddressId { get; set; }
    public Address? Address { get; set; }
    public int? LugarId { get; set; }
    public Lugar? Lugar { get; set; }
    public Turno Turno { get; set; } = Turno.CualquierHora;
    /// <summary>Latest time to be there ("Antes: a las 2:00 p.m."); closes the window of its turno earlier.</summary>
    public TimeOnly? Before { get; set; }
    /// <summary>Order in the ruta; 0 until the ruta is optimized.</summary>
    public int Position { get; set; }
    /// <summary>Moved by hand: optimizing keeps it in <see cref="Position"/> (docs/03-reglas-de-negocio.md, Rutas, rule 4).</summary>
    public bool PositionFixed { get; set; }
    public ParadaState State { get; set; } = ParadaState.Pendiente;
    /// <summary>When the chofer marked "en camino" (UTC).</summary>
    public DateTimeOffset? EnCaminoAt { get; set; }
    /// <summary>When the chofer marked "llegué" (UTC): the real time.</summary>
    public DateTimeOffset? ArrivedAt { get; set; }
    /// <summary>Where the phone was when marking "llegué"; null without GPS.</summary>
    public NetTopologySuite.Geometries.Point? ArrivedLocation { get; set; }
    /// <summary>Meters from the parada when marking "llegué"; null when unknown.</summary>
    public int? ArrivedDistanceMeters { get; set; }
    /// <summary>The chofer confirmed "llegué" farther than Gps.ToleranciaMetros; the office reviews it.</summary>
    public bool ArrivedOutsideRadius { get; set; }
    public string? Notes { get; set; }
    /// <summary>Pinned first or last; routing arranges the rest.</summary>
    public ParadaPlacement Placement { get; set; } = ParadaPlacement.Automatica;
    /// <summary>Tipo de parada; null for an invitado's recolección or entrega (cliente final).</summary>
    public ParadaCategory? Category { get; set; }
    /// <summary>The parada fija that generated it, if any.</summary>
    public int? ParadaFijaId { get; set; }
    public ParadaFija? ParadaFija { get; set; }
    public int? ViajeId { get; set; }
    public Viaje? Viaje { get; set; }
    /// <summary>1, 2, 3… inside its <see cref="Viaje"/>; the steps are visited in this order.</summary>
    public int? ViajeStep { get; set; }
    /// <summary>What the chofer sees as the name when there is no invitado (the parada fija's name).</summary>
    public string? Title { get; set; }
    /// <summary>What is done there.</summary>
    public string? Activity { get; set; }
    /// <summary>Address when it is neither an invitado's address nor a lugar.</summary>
    public string? AddressText { get; set; }
    /// <summary>Point in SRID 4326 for <see cref="AddressText"/>.</summary>
    public NetTopologySuite.Geometries.Point? Location { get; set; }

    /// <summary>When the chofer closed the visit, done or not done (UTC).</summary>
    public DateTimeOffset? ClosedAt { get; set; }
    public NetTopologySuite.Geometries.Point? ClosedLocation { get; set; }
    /// <summary>Prendas the chofer counted at a recolección.</summary>
    public int? GarmentsCollected { get; set; }
    /// <summary>Chofer's notes when closing, or the detail of why it was not done.</summary>
    public string? CloseNotes { get; set; }
    /// <summary>Motivo of the catalog when the visit was not done.</summary>
    public int? NoRealizadaReasonId { get; set; }
    /// <summary>Id the app gave the closing, so a resend after losing signal is answered without closing twice.</summary>
    public string? CloseLocalId { get; set; }

    public List<Evidence> Evidence { get; set; } = [];

    /// <summary>Leaving, coming back and lunch do not take cupo; every other parada does, fixed ones included.</summary>
    public static readonly ParadaKind[] KindsWithoutCupo = [ParadaKind.Salida, ParadaKind.Regreso, ParadaKind.Comida];

    public static bool TakesCupo(ParadaKind kind) => !KindsWithoutCupo.Contains(kind);
}

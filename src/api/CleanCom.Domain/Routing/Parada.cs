using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Routing;

/// <summary>
/// What the chofer does at a parada. Not to be confused with the <see cref="ParadaType"/> catalog
/// (Convenio, Insumos, Traspaso), which describes fixed paradas.
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
    public int? DireccionId { get; set; }
    public Direccion? Direccion { get; set; }
    public int? LugarId { get; set; }
    public Lugar? Lugar { get; set; }
    public Turno Turno { get; set; } = Turno.CualquierHora;
    /// <summary>Order in the ruta; 0 until the ruta is optimized.</summary>
    public int Position { get; set; }
    public ParadaState State { get; set; } = ParadaState.Pendiente;
    public string? Notes { get; set; }

    /// <summary>Leaving, coming back and lunch do not take cupo; every other parada does, fixed ones included.</summary>
    public static readonly ParadaKind[] KindsWithoutCupo = [ParadaKind.Salida, ParadaKind.Regreso, ParadaKind.Comida];

    public static bool TakesCupo(ParadaKind kind) => !KindsWithoutCupo.Contains(kind);
}

using CleanCom.Domain.Common;

namespace CleanCom.Domain.Routing;

/// <summary>
/// Pasos of one ruta day that are visited in order and one after the other (Florida → Del Valle → Florida).
/// Comes from a vuelta fija or from a "viaje de un día" added in Rutas. Its paradas carry <see cref="Parada.ViajeStep"/>.
/// </summary>
public class Viaje : Entity, IActivatable
{
    public int RutaDiaId { get; set; }
    public RutaDia? RutaDia { get; set; }
    public string Name { get; set; } = "";
    /// <summary>What is carried along the viaje.</summary>
    public string? Carried { get; set; }
    public int? ParadaFijaId { get; set; }
    public bool Active { get; set; } = true;
}

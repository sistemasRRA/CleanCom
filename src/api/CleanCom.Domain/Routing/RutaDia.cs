using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Routing;

/// <summary>
/// A ruta on a given date: one per ruta and date. Created the first time something is scheduled for that day.
/// Kilometers and shift times arrive with the chofer app (week 7).
/// </summary>
public class RutaDia : Entity
{
    public int RutaId { get; set; }
    public Ruta? Ruta { get; set; }
    public DateOnly Date { get; set; }
    public int? OperadorId { get; set; }
    public Operador? Operador { get; set; }
    /// <summary>The vehicle that does the ruta that day.</summary>
    public int? UnidadId { get; set; }
    public CleanCom.Domain.Fleet.Unidad? Unidad { get; set; }

    public List<Parada> Paradas { get; set; } = [];
    public List<Viaje> Viajes { get; set; } = [];
}

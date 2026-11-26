using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.People;

namespace CleanCom.Domain.Routing;

/// <summary>
/// A ruta on a given date: one per ruta and date. Created the first time something is scheduled for that day, or when
/// the chofer starts the shift.
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

    /// <summary>Inicio de turno in the chofer app, with the odometer reading.</summary>
    public DateTimeOffset? ShiftStartedAt { get; set; }
    public int? StartKm { get; set; }
    /// <summary>Local id of the app's "iniciar turno"; a resend after losing signal is answered without an error.</summary>
    public string? ShiftStartLocalId { get; set; }
    /// <summary>Fin de turno: back at the base, with the odometer reading and the declared payments (CorteCaja).</summary>
    public DateTimeOffset? ShiftEndedAt { get; set; }
    public int? EndKm { get; set; }
    public string? ShiftEndLocalId { get; set; }
    /// <summary>User of the office who closed the shift for the chofer (permiso cerrar-turnos); null when the chofer did.</summary>
    public int? ShiftEndedBy { get; set; }

    public List<Parada> Paradas { get; set; } = [];
    public List<Viaje> Viajes { get; set; } = [];
}

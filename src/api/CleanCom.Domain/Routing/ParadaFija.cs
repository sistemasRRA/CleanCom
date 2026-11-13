using CleanCom.Domain.Catalogs;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using NetTopologySuite.Geometries;

namespace CleanCom.Domain.Routing;

/// <summary>Where a parada goes in the ruta: routing decides, or it is pinned first or last.</summary>
public enum ParadaPlacement
{
    Automatica,
    Primera,
    Ultima,
}

/// <summary>
/// Work repeated every week: convenios, insumos and traspasos (docs/03-reglas-de-negocio.md, Paradas fijas).
/// One record with its pasos in order: a single paso is a fixed parada, several pasos are a vuelta fija
/// (for example Traspaso K) that generates a <see cref="Viaje"/>. Generated paradas take cupo, one per paso.
/// </summary>
public class ParadaFija : Entity, IActivatable
{
    /// <summary>"Qué se hace" options of a parada fija.</summary>
    public static readonly ParadaKind[] AllowedKinds =
        [ParadaKind.Recoleccion, ParadaKind.Entrega, ParadaKind.RecoleccionYEntrega, ParadaKind.Traslado];

    public string Name { get; set; } = "";
    /// <summary>Tipo de parada.</summary>
    public ParadaCategory Category { get; set; }
    /// <summary>Qué se hace.</summary>
    public ParadaKind Kind { get; set; }
    public int RutaId { get; set; }
    public Ruta? Ruta { get; set; }
    public Turno Turno { get; set; } = Turno.CualquierHora;
    public ParadaPlacement Placement { get; set; } = ParadaPlacement.Automatica;
    /// <summary>What is carried: insumos or load. Plain text, there is no inventory.</summary>
    public string? Carried { get; set; }
    public string? SiteContact { get; set; }
    public string? SitePhone { get; set; }
    public int? GarmentsApprox { get; set; }
    public string? DriverNotes { get; set; }
    /// <summary>First day it applies; null = already in force.</summary>
    public DateOnly? ValidFrom { get; set; }
    /// <summary>Last day it applies; null = no end.</summary>
    public DateOnly? ValidUntil { get; set; }
    /// <summary>Paused: it stays active and configured, but generates nothing until resumed.</summary>
    public bool Paused { get; set; }
    public bool Active { get; set; } = true;

    /// <summary>Days of the week it is visited, stored as a list in one column.</summary>
    public List<DayOfWeek> Days { get; set; } = [];
    /// <summary>Includes pasos removed in an edit (inactive); use <see cref="ActivePasos"/>.</summary>
    public List<ParadaFijaPaso> Pasos { get; set; } = [];
    public List<ParadaFijaException> Exceptions { get; set; } = [];

    public IReadOnlyList<ParadaFijaPaso> ActivePasos => Pasos.Where(p => p.Active).OrderBy(p => p.Order).ToList();

    public bool IsVuelta => ActivePasos.Count > 1;

    /// <summary>
    /// Whether it generates paradas on <paramref name="date"/>: active, not paused, within its validity, one of its
    /// days, not a day off and not an exception date. <see cref="Exceptions"/> must be loaded.
    /// </summary>
    public bool AppliesOn(DateOnly date, IReadOnlySet<DateOnly> nonWorkingDays) =>
        Active
        && !Paused
        && (ValidFrom is null || date >= ValidFrom)
        && (ValidUntil is null || date <= ValidUntil)
        && Days.Contains(date.DayOfWeek)
        && !nonWorkingDays.Contains(date)
        && !Exceptions.Any(e => e.Active && e.Date == date);

    /// <summary>
    /// The paradas for one day of <paramref name="rutaDia"/>, pasos in order. A vuelta also gets its <see cref="Viaje"/>,
    /// which keeps the pasos together and in order when routing. <see cref="Pasos"/> must be loaded.
    /// </summary>
    public IReadOnlyList<Parada> BuildParadas(RutaDia rutaDia)
    {
        var pasos = ActivePasos;
        if (pasos.Count == 0)
        {
            throw new DomainException($"La parada fija {Name} no tiene pasos.");
        }

        var viaje = pasos.Count > 1 ? new Viaje { RutaDia = rutaDia, Name = Name, Carried = Carried, ParadaFijaId = Id } : null;
        return pasos.Select((paso, i) => new Parada
        {
            RutaDia = rutaDia,
            Kind = Kind,
            Category = Category,
            Turno = Turno,
            Placement = Placement,
            ParadaFijaId = Id,
            Viaje = viaje,
            ViajeStep = viaje is null ? null : i + 1,
            Title = Name,
            Activity = paso.Activity,
            LugarId = paso.LugarId,
            Address = paso.LugarId is null ? paso.Address : null,
            Location = paso.LugarId is null ? paso.Location : null,
            Notes = JoinNotes(DriverNotes, paso.Notes),
        }).ToList();
    }

    private static string? JoinNotes(string? general, string? paso) =>
        (general, paso) switch
        {
            (null, null) => null,
            (null, _) => paso,
            (_, null) => general,
            _ => $"{general} · {paso}",
        };
}

/// <summary>
/// A paso of a parada fija: a lugar from the catalog or an address with coordinates. A paso removed in an edit
/// is deactivated, never deleted.
/// </summary>
public class ParadaFijaPaso : Entity, IActivatable
{
    public bool Active { get; set; } = true;
    public int ParadaFijaId { get; set; }
    /// <summary>1, 2, 3… in visiting order.</summary>
    public int Order { get; set; }
    public int? LugarId { get; set; }
    public Lugar? Lugar { get; set; }
    public string? Address { get; set; }
    /// <summary>Point in SRID 4326 (<c>geography</c> in SQL Server).</summary>
    public Point? Location { get; set; }
    /// <summary>What is done there: "Recoger traspaso", "Entregar insumos".</summary>
    public string? Activity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>A date the parada fija is skipped. Removing it is a baja lógica.</summary>
public class ParadaFijaException : Entity, IActivatable
{
    public int ParadaFijaId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
    public bool Active { get; set; } = true;
}

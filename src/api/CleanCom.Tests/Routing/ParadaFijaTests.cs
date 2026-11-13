using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

/// <summary>docs/03-reglas-de-negocio.md, Paradas fijas, and docs/pantallas/oficina/paradas-fijas.md.</summary>
public class ParadaFijaTests
{
    // 16 nov 2026 is a Monday.
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly HashSet<DateOnly> NoDaysOff = [];

    private static ParadaFija TraspasoK() => new()
    {
        Id = 7,
        Name = "Traspaso K",
        Kind = ParadaKind.Traspaso,
        RutaId = 2,
        Turno = Turno.Manana,
        Placement = ParadaPlacement.Primera,
        Carried = "Prendas para planta",
        Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
        Pasos =
        [
            new ParadaFijaPaso { Order = 3, LugarId = 1, Activity = "Descargar" },
            new ParadaFijaPaso { Order = 1, LugarId = 1, Activity = "Cargar" },
            new ParadaFijaPaso { Order = 2, LugarId = 4, Activity = "Entregar traspaso" },
            new ParadaFijaPaso { Order = 4, LugarId = 9, Active = false },
        ],
    };

    [Fact]
    public void Traspaso_K_generates_monday_to_saturday_but_not_sunday()
    {
        var fija = TraspasoK();
        var week = Enumerable.Range(0, 7).Select(i => Monday.AddDays(i)).Where(d => fija.AppliesOn(d, NoDaysOff)).ToList();
        Assert.Equal(6, week.Count);
        Assert.DoesNotContain(week, d => d.DayOfWeek == DayOfWeek.Sunday);
    }

    [Fact]
    public void A_vuelta_generates_its_pasos_in_order_inside_one_viaje()
    {
        var rutaDia = new RutaDia { RutaId = 2, Date = Monday };
        var paradas = TraspasoK().BuildParadas(rutaDia);

        // 3 active pasos: the inactive one is left out; the agenda counts 3 places of the 14.
        Assert.Equal(3, paradas.Count);
        Assert.Equal(["Cargar", "Entregar traspaso", "Descargar"], paradas.Select(p => p.Activity));
        Assert.Equal([1, 2, 3], paradas.Select(p => p.ViajeStep!.Value));
        var viaje = Assert.Single(paradas.Select(p => p.Viaje).Distinct());
        Assert.NotNull(viaje);
        Assert.Equal("Traspaso K", viaje.Name);
        Assert.Equal("Prendas para planta", viaje.Carried);
        Assert.All(paradas, p =>
        {
            Assert.Same(rutaDia, p.RutaDia);
            Assert.Equal(7, p.ParadaFijaId);
            Assert.Equal(ParadaKind.Traspaso, p.Kind);
            Assert.Equal(ParadaPlacement.Primera, p.Placement);
            Assert.True(Parada.TakesCupo(p.Kind));
        });
    }

    [Fact]
    public void A_single_paso_is_a_parada_without_viaje()
    {
        var fija = new ParadaFija
        {
            Name = "Insumos Del Valle",
            Kind = ParadaKind.Insumos,
            DriverNotes = "Tocar en la puerta de servicio",
            Days = [DayOfWeek.Tuesday],
            Pasos = [new ParadaFijaPaso { Order = 1, Address = "Av. Coyoacán 100", Notes = "Preguntar por Ana" }],
        };
        var parada = Assert.Single(fija.BuildParadas(new RutaDia { Date = Monday.AddDays(1) }));
        Assert.Null(parada.Viaje);
        Assert.Null(parada.ViajeStep);
        Assert.Equal("Av. Coyoacán 100", parada.Address);
        Assert.Equal("Tocar en la puerta de servicio · Preguntar por Ana", parada.Notes);
    }

    [Fact]
    public void A_day_off_or_an_exception_date_does_not_generate()
    {
        var fija = TraspasoK();
        Assert.False(fija.AppliesOn(Monday, new HashSet<DateOnly> { Monday }));

        fija.Exceptions.Add(new ParadaFijaException { Date = Monday.AddDays(1) });
        Assert.False(fija.AppliesOn(Monday.AddDays(1), NoDaysOff));

        // A removed exception (baja lógica) no longer skips the date.
        fija.Exceptions.Add(new ParadaFijaException { Date = Monday.AddDays(2), Active = false });
        Assert.True(fija.AppliesOn(Monday.AddDays(2), NoDaysOff));
    }

    [Fact]
    public void Validity_pause_and_baja_stop_the_generation()
    {
        var fija = TraspasoK();
        fija.ValidFrom = Monday.AddDays(2);
        fija.ValidUntil = Monday.AddDays(3);
        Assert.False(fija.AppliesOn(Monday.AddDays(1), NoDaysOff));
        Assert.True(fija.AppliesOn(Monday.AddDays(2), NoDaysOff));
        Assert.True(fija.AppliesOn(Monday.AddDays(3), NoDaysOff));
        Assert.False(fija.AppliesOn(Monday.AddDays(4), NoDaysOff));

        fija.Paused = true;
        Assert.False(fija.AppliesOn(Monday.AddDays(2), NoDaysOff));
        fija.Paused = false;
        fija.Active = false;
        Assert.False(fija.AppliesOn(Monday.AddDays(2), NoDaysOff));
    }

    [Fact]
    public void Without_pasos_it_cannot_generate()
    {
        var fija = new ParadaFija { Name = "Sin pasos", Days = [DayOfWeek.Monday] };
        var error = Assert.Throws<DomainException>(() => fija.BuildParadas(new RutaDia { Date = Monday }));
        Assert.Contains("no tiene pasos", error.Message);
    }
}

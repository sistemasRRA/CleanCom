using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Routing;

public class InvitadoTrackingTests
{
    [Theory]
    [InlineData(ParadaState.EnCamino, true)]
    [InlineData(ParadaState.Pendiente, false)]
    [InlineData(ParadaState.EnSitio, false)]
    [InlineData(ParadaState.Realizada, false)]
    [InlineData(null, false)]
    public void The_chofer_is_only_shown_while_en_camino(ParadaState? state, bool shown) =>
        Assert.Equal(shown, InvitadoTracking.ShowsChofer(state));

    [Fact]
    public void Arrival_is_the_street_distance_at_the_average_speed_from_the_last_reading()
    {
        // About 3 km in a straight line, × 1.4 = 4.2 km at 18 km/h = 14 minutes.
        var chofer = new GeoPoint(19.4000, -99.1700);
        var home = new GeoPoint(19.4269, -99.1700);
        var at = new DateTimeOffset(2026, 11, 23, 17, 0, 0, TimeSpan.Zero); // 11:00 in Mexico City.

        var arrival = InvitadoTracking.EstimatedArrival(chofer, at, home, 18, 1.4);

        Assert.Equal(at.AddMinutes(14), arrival);
    }

    [Theory]
    [InlineData("Luis Pérez Gómez", "Luis")]
    [InlineData("  Ana ", "Ana")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Only_the_first_name_of_the_chofer_is_given(string? name, string? expected) =>
        Assert.Equal(expected, InvitadoTracking.FirstName(name));
}

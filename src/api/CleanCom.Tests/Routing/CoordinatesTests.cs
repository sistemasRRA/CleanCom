using CleanCom.Domain.Geography;

namespace CleanCom.Tests.Routing;

/// <summary>"Link de Google Maps o coordenadas" in paradas fijas.</summary>
public class CoordinatesTests
{
    [Theory]
    [InlineData("19.3608092,-99.1968494")]
    [InlineData(" 19.3608092 , -99.1968494 ")]
    [InlineData("https://www.google.com/maps/place/Leones/@19.3608092,-99.1968494,17z/data=!3m1")]
    [InlineData("https://www.google.com/maps/place/Leones/data=!4m6!3m5!1s0x0:0x0!8m2!3d19.3608092!4d-99.1968494")]
    [InlineData("https://maps.google.com/?q=19.3608092,-99.1968494")]
    [InlineData("https://www.google.com/maps/search/?api=1&query=19.3608092%2C-99.1968494")]
    public void Reads_coordinates_from_text_or_a_full_link(string text)
    {
        Assert.True(Geo.TryParseCoordinates(text, out var lat, out var lng));
        Assert.Equal(19.3608092, lat, 6);
        Assert.Equal(-99.1968494, lng, 6);
    }

    [Theory]
    [InlineData("https://maps.app.goo.gl/AbCdEf123")]
    [InlineData("Calz. de los Leones 165")]
    [InlineData("95.1,-99.1")]
    [InlineData("")]
    public void A_short_link_or_text_without_coordinates_is_not_read(string text)
    {
        Assert.False(Geo.TryParseCoordinates(text, out _, out _));
    }
}

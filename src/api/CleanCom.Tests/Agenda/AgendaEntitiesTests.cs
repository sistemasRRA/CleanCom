using CleanCom.Domain.Common;
using CleanCom.Domain.People;
using CleanCom.Domain.Routing;

namespace CleanCom.Tests.Agenda;

/// <summary>
/// docs/02-modelo-de-datos.md: which paradas take cupo, and one invitado per WhatsApp number.
/// </summary>
public class AgendaEntitiesTests
{
    [Theory]
    [InlineData(ParadaKind.Recoleccion)]
    [InlineData(ParadaKind.Entrega)]
    [InlineData(ParadaKind.RecoleccionYEntrega)]
    [InlineData(ParadaKind.Convenio)]
    [InlineData(ParadaKind.Insumos)]
    [InlineData(ParadaKind.Traspaso)]
    public void Visits_and_fixed_paradas_take_cupo(ParadaKind kind)
    {
        Assert.True(Parada.TakesCupo(kind));
    }

    [Theory]
    [InlineData(ParadaKind.Salida)]
    [InlineData(ParadaKind.Regreso)]
    [InlineData(ParadaKind.Comida)]
    public void Leaving_coming_back_and_lunch_do_not_take_cupo(ParadaKind kind)
    {
        Assert.False(Parada.TakesCupo(kind));
    }

    [Theory]
    [InlineData("5512345678")]
    [InlineData("55 1234 5678")]
    [InlineData("(55) 1234-5678")]
    [InlineData("+52 55 1234 5678")]
    [InlineData("+52 1 55 1234 5678")]
    public void Any_format_of_the_same_phone_is_the_same_invitado(string phone)
    {
        Assert.Equal("5512345678", Phones.Normalize(phone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("551234567890123")]
    public void A_phone_without_10_digits_is_rejected(string phone)
    {
        Assert.Throws<DomainException>(() => Phones.Normalize(phone));
    }
}

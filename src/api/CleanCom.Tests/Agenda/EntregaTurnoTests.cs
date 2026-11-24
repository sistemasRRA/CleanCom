using CleanCom.Domain.Agenda;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;

namespace CleanCom.Tests.Agenda;

/// <summary>Turno of the entrega in Programar entrega (docs/03-reglas-de-negocio.md, Pedidos).</summary>
public class EntregaTurnoTests
{
    [Theory]
    [InlineData(null, Turno.Manana, Turno.Manana)]
    [InlineData(null, Turno.HoraEspecifica, Turno.CualquierHora)]
    [InlineData(Turno.Tarde, Turno.Manana, Turno.Tarde)]
    [InlineData(Turno.CualquierHora, Turno.Tarde, Turno.CualquierHora)]
    public void Keeps_the_last_entrega_turno_or_takes_the_recoleccion_one(Turno? lastEntrega, Turno recoleccion, Turno expected)
    {
        Assert.Equal(expected, EntregaTurno.Default(lastEntrega, recoleccion));
    }

    [Fact]
    public void An_entrega_has_no_specific_time()
    {
        Assert.Contains("hora específica", Assert.Throws<DomainException>(() => EntregaTurno.EnsureAllowed(Turno.HoraEspecifica)).Message);
        EntregaTurno.EnsureAllowed(Turno.Tarde);
        EntregaTurno.EnsureAllowed(null);
    }
}

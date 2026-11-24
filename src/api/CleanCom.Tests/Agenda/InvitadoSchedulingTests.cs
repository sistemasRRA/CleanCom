using CleanCom.Domain.Agenda;
using CleanCom.Domain.Routing;
using CleanCom.Domain.Time;
using CleanCom.Domain.Common;
using CleanCom.Domain.Pedidos;

namespace CleanCom.Tests.Agenda;

/// <summary>Agenda from the invitado app: "Hora específica y prioridad solo las puede usar oficina".</summary>
public class InvitadoSchedulingTests
{
    [Theory]
    [InlineData(Turno.CualquierHora)]
    [InlineData(Turno.Manana)]
    [InlineData(Turno.Tarde)]
    public void The_app_can_choose_a_turno(Turno turno)
    {
        InvitadoScheduling.EnsureAllowed(turno, null, false, null, null);
    }

    [Fact]
    public void Specific_time_priority_hora_de_registro_and_a_hand_picked_zona_are_office_only()
    {
        Assert.Contains("hora específica", Assert.Throws<DomainException>(() =>
            InvitadoScheduling.EnsureAllowed(Turno.HoraEspecifica, new TimeOnly(10, 0), false, null, null)).Message);
        Assert.Contains("prioridad", Assert.Throws<DomainException>(() =>
            InvitadoScheduling.EnsureAllowed(Turno.Manana, null, true, null, null)).Message);
        // The hora de registro is automatic: an earlier one could get past the 13:00 cutoff.
        Assert.Contains("hora de registro", Assert.Throws<DomainException>(() =>
            InvitadoScheduling.EnsureAllowed(Turno.Manana, null, false, DateTimeOffset.UnixEpoch, null)).Message);
        Assert.Contains("zona", Assert.Throws<DomainException>(() =>
            InvitadoScheduling.EnsureAllowed(Turno.Manana, null, false, null, 3)).Message);
    }

    private static readonly TimeOnly Corte13 = new(13, 0);

    /// <summary>Instant from a Mexico City date and time in November 2026.</summary>
    private static DateTimeOffset Nov(int day, int hour, int minute) =>
        BusinessTime.ToUtc(new DateOnly(2026, 11, day), new TimeOnly(hour, minute));

    [Theory]
    [InlineData(PedidoEtapa.Agendado, null, true)]
    [InlineData(PedidoEtapa.Agendado, ParadaState.Pendiente, true)]
    [InlineData(PedidoEtapa.Agendado, ParadaState.EnCamino, false)]
    [InlineData(PedidoEtapa.Agendado, ParadaState.EnSitio, false)]
    [InlineData(PedidoEtapa.Recolectado, null, false)]
    [InlineData(PedidoEtapa.Cancelado, null, false)]
    public void Only_a_recoleccion_still_to_be_done_changes_day(PedidoEtapa etapa, ParadaState? state, bool changeable)
    {
        Assert.Equal(changeable, InvitadoScheduling.IsChangeable(etapa, state));
        if (changeable)
        {
            InvitadoScheduling.EnsureCanChangeDay(etapa, state);
        }
        else
        {
            Assert.Throws<DomainException>(() => InvitadoScheduling.EnsureCanChangeDay(etapa, state));
        }
    }

    [Fact]
    public void Cancels_until_13_of_the_day_before_then_only_the_office()
    {
        // Recolección el día 5: cancela hasta el día 4 a las 13:00.
        var day5 = new DateOnly(2026, 11, 5);
        InvitadoScheduling.EnsureCanCancel(PedidoEtapa.Agendado, ParadaState.Pendiente, Nov(4, 13, 0), day5, Corte13);
        var late = Assert.Throws<DomainException>(() =>
            InvitadoScheduling.EnsureCanCancel(PedidoEtapa.Agendado, ParadaState.Pendiente, Nov(4, 13, 1), day5, Corte13));
        Assert.Contains("13:00", late.Message);
        Assert.Contains("oficina", late.Message);
        Assert.False(InvitadoScheduling.CanCancel(PedidoEtapa.Agendado, ParadaState.Pendiente, Nov(4, 13, 1), day5, Corte13));
        // Before the cutoff but already collected: no.
        Assert.False(InvitadoScheduling.CanCancel(PedidoEtapa.Recolectado, null, Nov(3, 9, 0), day5, Corte13));
    }
}

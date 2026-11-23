using CleanCom.Domain.Agenda;
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
}

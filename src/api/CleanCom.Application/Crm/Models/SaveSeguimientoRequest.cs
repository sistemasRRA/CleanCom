using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <param name="ManualStatus">Empty = calculated automatically.</param>
/// <param name="FixedFrequencyDays">Empty = calculated from their services.</param>
public record SaveSeguimientoRequest(CrmStatus? ManualStatus, int? FixedFrequencyDays, DateOnly? NextContactOn, bool DoNotContact);

using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <summary>The ficha: data, CRM evaluation, pedidos, contact history, what the invitado is worth and their app account.</summary>
public record CrmFichaDto(
    InvitadoDto Invitado,
    CrmRowDto Crm,
    CrmStatus? ManualStatus,
    int? FixedFrequencyDays,
    IReadOnlyList<PedidoDto> Pedidos,
    IReadOnlyList<ContactDto> Contacts,
    CrmValueDto Value,
    AppAccountDto Account);

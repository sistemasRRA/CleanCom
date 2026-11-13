using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <summary>The ficha: data, CRM evaluation, pedidos and contact history.</summary>
public record CrmFichaDto(
    InvitadoDto Invitado,
    CrmRowDto Crm,
    CrmStatus? ManualStatus,
    int? FixedFrequencyDays,
    IReadOnlyList<PedidoDto> Pedidos,
    IReadOnlyList<ContactoDto> Contactos);

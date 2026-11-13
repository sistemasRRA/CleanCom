using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <param name="Counts">By status, without tiendas (they do not count in CRM metrics).</param>
/// <param name="CallTodayCount">How many are in "Llamar hoy", whatever the filters.</param>
public record CrmListDto(DateOnly Today, int Total, int CallTodayCount, IReadOnlyList<CrmStatusCountDto> Counts, IReadOnlyList<CrmRowDto> Rows);

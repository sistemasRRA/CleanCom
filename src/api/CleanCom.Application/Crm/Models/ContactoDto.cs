using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

public record ContactoDto(int Id, ContactChannel Channel, ContactResult Result, string? Note, DateOnly? NextContactOn, DateTimeOffset ContactedAt);

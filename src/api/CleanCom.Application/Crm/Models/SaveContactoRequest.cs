using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <param name="NextContactOn">Empty = calculated from the result (tomorrow, in 3 days or in 3 weeks).</param>
public record SaveContactoRequest(ContactChannel Channel, ContactResult Result, string? Note, DateOnly? NextContactOn);

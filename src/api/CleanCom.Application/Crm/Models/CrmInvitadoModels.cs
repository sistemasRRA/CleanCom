using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <summary>What the invitado is worth: their tickets (POS) and services.</summary>
/// <param name="TotalSpent">Amount of all their tickets.</param>
/// <param name="AverageTicket">TotalSpent between the tickets with an amount; null without tickets.</param>
/// <param name="Services">Pedidos not cancelled up to today (the CRM's services).</param>
public record CrmValueDto(decimal TotalSpent, int Tickets, decimal? AverageTicket, int Services, DateOnly? FirstService, DateOnly? LastService);

/// <summary>The invitado's app account.</summary>
/// <param name="LastAccess">The last time they signed in or opened a session.</param>
/// <param name="InvitedAt">When the office last sent them the invitation.</param>
public record AppAccountDto(AppAccountState State, DateTimeOffset? LastAccess, DateTimeOffset? InvitedAt);

public enum HistoriaKind
{
    Pedido,
    Visita,
    Ticket,
    Aviso,
    Contacto,
}

/// <summary>One line of the invitado's history, newest first.</summary>
/// <param name="Detail">A second line (notes, state of the aviso, motivo).</param>
/// <param name="Amount">Money: the ticket or the reference payment of the visit.</param>
public record HistoriaItemDto(DateTimeOffset At, HistoriaKind Kind, string Text, string? Detail, decimal? Amount, int? PedidoId);

/// <summary>A possible duplicate: another active invitado who shares a phone.</summary>
public record DuplicateDto(int Id, string Name, string WhatsAppPhone, string? OtherPhone, string? Email, int Pedidos, bool HasApp, DateTimeOffset CreatedAt);

/// <summary>Merges <paramref name="DuplicadoId"/> into the invitado of the URL, which stays.</summary>
public record MergeRequest(int DuplicadoId);

/// <summary>The invitation to the app: the WhatsApp text and the link that opens it with that text.</summary>
public record AppInvitationDto(string Text, string WhatsAppUrl);

/// <param name="Blocked">True blocks the access to the app; false gives it back.</param>
public record BlockAppRequest(bool Blocked);

using CleanCom.Application.Invitados;
using CleanCom.Application.Pedidos;
using CleanCom.Domain.Crm;
using CleanCom.Domain.People;

namespace CleanCom.Application.Crm;

/// <summary>A row of the CRM list.</summary>
/// <param name="StatusIsManual">The office fixed the status; <paramref name="Why"/> says what was calculated.</param>
/// <param name="SuggestedVisit">Next visit day of their zona from the "le toca" date.</param>
/// <param name="NextRecoleccion">Next scheduled recolección, if any: worth knowing before calling.</param>
public record CrmRowDto(
    int Id,
    string Name,
    string WhatsAppPhone,
    InvitadoType Type,
    string? Neighborhood,
    string? ZonaCode,
    string? RutaCode,
    CrmStatus Status,
    bool StatusIsManual,
    string Why,
    int ServiceCount,
    int? FrequencyDays,
    DateOnly? LastService,
    DateOnly? LeTocaDate,
    DateOnly? SuggestedVisit,
    DateOnly? NextRecoleccion,
    DateOnly? NextContactOn,
    bool DoNotContact);

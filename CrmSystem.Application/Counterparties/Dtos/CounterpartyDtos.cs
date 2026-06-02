using CrmSystem.Domain;

namespace CrmSystem.Application.Counterparties.Dtos;

public sealed record CounterpartyDto(
    Guid OrganizationId,
    string Name,
    string? FullName,
    CounterpartyType Type,
    CounterpartyStatus Status,
    string? Inn,
    string? Kpp,
    string? Ogrn,
    int? PaymentTermsDays);

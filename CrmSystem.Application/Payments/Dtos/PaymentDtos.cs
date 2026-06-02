using CrmSystem.Domain;

namespace CrmSystem.Application.Payments.Dtos;

public sealed record PaymentDto(
    PaymentType Type,
    Guid OrganizationId,
    Guid? CounterpartyId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? BankReference,
    string? ExternalId,
    PaymentStatus Status);

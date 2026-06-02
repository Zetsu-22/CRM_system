using CrmSystem.Domain;

namespace CrmSystem.Application.Upds.Dtos;

public sealed record UpdDto(
    string? Number,
    DateOnly Date,
    Guid OrganizationId,
    Guid CounterpartyId,
    Guid? OrderId,
    Guid? InvoiceId,
    UpdFunction Function,
    UpdStatus Status,
    DateOnly? ShippingDate,
    string? Comment,
    List<UpdLineDto> Lines);

public sealed record UpdLineDto(
    Guid ItemId,
    string NameSnapshot,
    VatRate VatRateSnapshot,
    decimal Qty,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue);

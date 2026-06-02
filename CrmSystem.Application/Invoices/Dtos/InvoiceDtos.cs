using CrmSystem.Domain;

namespace CrmSystem.Application.Invoices.Dtos;

public sealed record InvoiceDto(
    string? Number,
    DateOnly Date,
    Guid OrganizationId,
    Guid CounterpartyId,
    Guid? OrderId,
    InvoiceStatus Status,
    string? Comment,
    List<InvoiceLineDto> Lines);

public sealed record InvoiceLineDto(
    Guid ItemId,
    string NameSnapshot,
    VatRate VatRateSnapshot,
    decimal Qty,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue);

using CrmSystem.Domain;

namespace CrmSystem.Application.Items.Dtos;

public sealed record ItemDto(
    Guid OrganizationId,
    ItemType Type,
    string? Sku,
    string? Barcode,
    string Name,
    string? Description,
    string Unit,
    VatRate VatRate);

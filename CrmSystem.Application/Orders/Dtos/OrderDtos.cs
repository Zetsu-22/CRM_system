using CrmSystem.Domain;

namespace CrmSystem.Application.Orders.Dtos;

public sealed record SalesOrderDto(
    string? Number,
    DateOnly Date,
    Guid OrganizationId,
    Guid CounterpartyId,
    string? Comment,
    Guid? PickerId,
    Guid? DriverId,
    string? BankAccount,
    string? Contract,
    string? DeliveryAddress,
    List<SalesOrderLineDto> Lines,
    uint? RowVersion = null);

public sealed record SalesOrderLineDto(
    Guid ItemId,
    string NameSnapshot,
    VatRate VatRateSnapshot,
    decimal Qty,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue);

public sealed record OrderLineResponse(
    Guid Id,
    Guid ItemId,
    string? ItemName,
    string NameSnapshot,
    VatRate VatRateSnapshot,
    decimal Qty,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal Net,
    decimal Vat,
    decimal Gross);

public sealed record OrderResponse(
    Guid Id,
    string? Number,
    DateOnly Date,
    Guid CompanyId,
    string? CompanyName,
    Guid OrganizationId,
    string? OrganizationName,
    Guid CounterpartyId,
    string? CounterpartyName,
    OrderStatus Status,
    string? Comment,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    bool IsAssemblyChecklistClosed,
    Guid? PickerId,
    Guid? DriverId,
    string? BankAccount,
    string? Contract,
    string? DeliveryAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<OrderLineResponse> Lines,
    uint RowVersion,
    // Phase 4.2 — снапшоты реквизитов
    string? BankAccountSnapshotJson,
    string? DeliveryAddressSnapshotJson,
    // Phase 4.4 — чек-лист
    string? PickingChecklistJson);

public sealed record OrderStatusResponse(Guid Id, OrderStatus Status);

public sealed record AssemblyChecklistResponse(Guid Id, bool IsAssemblyChecklistClosed);

// ── Phase 4.1 — история статусов ──────────────────────────────────────────

public sealed record OrderStatusHistoryEntry(
    Guid Id,
    OrderStatus Status,
    string StatusName,
    DateTimeOffset ChangedAt,
    Guid? ChangedBy,
    string? Comment);

// ── Phase 4.4 — чек-лист сборки ───────────────────────────────────────────

public sealed record PickingChecklistItem(
    Guid ItemId,
    string Name,
    decimal Qty,
    bool Done,
    Guid? DoneBy,
    DateTimeOffset? DoneAt);

public sealed record PickingChecklistResponse(
    Guid OrderId,
    bool IsFullyClosed,
    List<PickingChecklistItem> Items);

public sealed record MarkPickingItemDoneRequest(Guid? DoneBy);

/// <summary>Тип доставки заказа — определяет ветку статусной машины.</summary>
public static class DeliveryTypes
{
    public const string SelfPickup       = "selfPickup";
    public const string CompanyDelivery  = "companyDelivery";
    public const string TransportCompany = "transportCompany";
}

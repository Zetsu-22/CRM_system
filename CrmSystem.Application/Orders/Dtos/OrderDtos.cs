using CrmSystem.Domain;

namespace CrmSystem.Application.Orders.Dtos;

public sealed record SalesOrderDto(
    string? Number,
    DateOnly Date,
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
    decimal DiscountValue,
    Guid? WarehouseId);

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
    decimal Gross,
    Guid? WarehouseId,
    decimal AvailableQty);

public sealed record OrderResponse(
    Guid Id,
    string? Number,
    DateOnly Date,
    Guid CompanyId,
    string? CompanyName,
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
    uint RowVersion);

public sealed record OrderStatusResponse(Guid Id, OrderStatus Status);

public sealed record AssemblyChecklistResponse(Guid Id, bool IsAssemblyChecklistClosed);

/// <summary>Тип доставки заказа — определяет ветку статусной машины.</summary>
public static class DeliveryTypes
{
    public const string SelfPickup       = "selfPickup";
    public const string CompanyDelivery  = "companyDelivery";
    public const string TransportCompany = "transportCompany";
}

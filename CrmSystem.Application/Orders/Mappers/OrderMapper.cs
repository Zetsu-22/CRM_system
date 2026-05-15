using CrmSystem.Application.Orders.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Orders.Mappers;

/// <summary>SalesOrder → OrderResponse. Нужны загруженные Company/Counterparty/Lines/Item.</summary>
public static class OrderMapper
{
    public static OrderResponse ToResponse(SalesOrder o, IReadOnlyDictionary<Guid, decimal> availableByItemId) => new(
        o.Id, o.Number, o.Date, o.CompanyId, o.Company?.Name,
        o.CounterpartyId, o.Counterparty?.Name,
        o.Status, o.Comment, o.TotalNet, o.TotalVat, o.TotalGross,
        o.IsAssemblyChecklistClosed,
        o.PickerId, o.DriverId, o.BankAccount, o.Contract, o.DeliveryAddress,
        o.CreatedAt, o.UpdatedAt,
        o.Lines.Select(l =>
        {
            availableByItemId.TryGetValue(l.ItemId, out var available);
            return new OrderLineResponse(
                l.Id, l.ItemId, l.Item?.Name, l.NameSnapshot, l.VatRateSnapshot,
                l.Qty, l.UnitPrice, l.DiscountType, l.DiscountValue,
                l.Net, l.Vat, l.Gross, l.WarehouseId,
                available);
        }).ToList(),
        o.RowVersion);
}

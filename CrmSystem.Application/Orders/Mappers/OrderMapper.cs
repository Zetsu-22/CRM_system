using CrmSystem.Application.Orders.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Orders.Mappers;

public static class OrderMapper
{
    public static OrderResponse ToResponse(SalesOrder o) => new(
        o.Id, o.Number, o.Date, o.CompanyId, o.Company?.Name,
        o.OrganizationId, o.Organization?.Name,
        o.CounterpartyId, o.Counterparty?.Name,
        o.Status, o.Comment, o.TotalNet, o.TotalVat, o.TotalGross,
        o.IsAssemblyChecklistClosed,
        o.PickerId, o.DriverId, o.BankAccount, o.Contract, o.DeliveryAddress,
        o.CreatedAt, o.UpdatedAt,
        o.Lines.Select(l => new OrderLineResponse(
            l.Id, l.ItemId, l.Item?.Name, l.NameSnapshot, l.VatRateSnapshot,
            l.Qty, l.UnitPrice, l.DiscountType, l.DiscountValue,
            l.Net, l.Vat, l.Gross)).ToList(),
        o.RowVersion,
        o.BankAccountSnapshotJson,
        o.DeliveryAddressSnapshotJson,
        o.PickingChecklistJson);
}

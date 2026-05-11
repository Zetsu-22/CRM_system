namespace CrmSystem.Domain;

public sealed class UpdLine : AuditableEntity
{
    public Guid UpdId { get; set; }
    public Upd? Upd { get; set; }

    public Guid ItemId { get; set; }

    public required string NameSnapshot { get; set; }
    public VatRate VatRateSnapshot { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal Gross { get; set; }

    public Guid? WarehouseId { get; set; }
}

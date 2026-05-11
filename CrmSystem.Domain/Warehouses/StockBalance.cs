namespace CrmSystem.Domain;

public sealed class StockBalance : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public decimal QtyOnHand { get; set; }
    public decimal QtyReserved { get; set; }
}

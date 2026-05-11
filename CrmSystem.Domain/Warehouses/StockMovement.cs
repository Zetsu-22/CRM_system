namespace CrmSystem.Domain;

public enum StockMovementSourceType
{
    Order = 1,
    Upd = 2,
    Manual = 3
}

public sealed class StockMovement : AuditableEntity
{
    public DateTimeOffset Date { get; set; }

    public Guid WarehouseId { get; set; }
    public Guid ItemId { get; set; }

    public decimal QtyDeltaOnHand { get; set; }
    public decimal QtyDeltaReserved { get; set; }

    public StockMovementSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
}

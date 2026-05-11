namespace CrmSystem.Domain;

public sealed class Item : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public ItemType Type { get; set; }
    public required string Sku { get; set; }
    public string? Barcode { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public required string Unit { get; set; }
    public VatRate VatRate { get; set; }

    public bool IsStockItem { get; set; }
}

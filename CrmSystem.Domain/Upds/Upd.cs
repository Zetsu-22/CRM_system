namespace CrmSystem.Domain;

public sealed class Upd : AuditableEntity, ITenantOwned
{
    public string? Number { get; set; }
    public DateOnly Date { get; set; }

    public Guid CompanyId { get; set; }
    public Guid CounterpartyId { get; set; }

    public Guid? OrderId { get; set; }
    public Guid? InvoiceId { get; set; }

    public UpdFunction Function { get; set; }
    public UpdStatus Status { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public string? Comment { get; set; }

    public ICollection<UpdLine> Lines { get; set; } = new List<UpdLine>();
}

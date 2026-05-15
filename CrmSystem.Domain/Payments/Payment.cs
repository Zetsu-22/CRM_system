namespace CrmSystem.Domain;

public sealed class Payment : AuditableEntity, ITenantOwned
{
    public PaymentType Type { get; set; }

    public Guid CompanyId { get; set; }
    public Guid? CounterpartyId { get; set; }

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public string? BankReference { get; set; }
    public string? ExternalId { get; set; }

    public PaymentStatus Status { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}

namespace CrmSystem.Domain;

public sealed class PaymentAllocation : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
}

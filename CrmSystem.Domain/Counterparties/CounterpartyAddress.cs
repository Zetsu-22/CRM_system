namespace CrmSystem.Domain;

public enum CounterpartyAddressType
{
    Shipping = 1,
    Billing = 2
}

public sealed class CounterpartyAddress : AuditableEntity
{
    public Guid CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    public CounterpartyAddressType Type { get; set; }
    public required string AddressLine { get; set; }
}

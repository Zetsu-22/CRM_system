namespace CrmSystem.Domain;

public sealed class CounterpartyContact : AuditableEntity
{
    public Guid CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

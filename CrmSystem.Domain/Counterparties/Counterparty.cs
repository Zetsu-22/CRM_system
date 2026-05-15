namespace CrmSystem.Domain;

public sealed class Counterparty : AuditableEntity, ITenantOwned
{
    public Guid CompanyId { get; set; }

    public CounterpartyType Type { get; set; }
    public CounterpartyStatus Status { get; set; }

    public required string Name { get; set; }
    public string? FullName { get; set; }

    public string? Inn { get; set; }
    public string? Kpp { get; set; }
    public string? Ogrn { get; set; }

    public int? PaymentTermsDays { get; set; }

    public ICollection<CounterpartyAddress> Addresses { get; set; } = new List<CounterpartyAddress>();
    public ICollection<CounterpartyContact> Contacts { get; set; } = new List<CounterpartyContact>();
}

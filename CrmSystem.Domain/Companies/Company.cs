namespace CrmSystem.Domain;

public sealed class Company : AuditableEntity
{
    public required string Name { get; set; }
    public string? Inn { get; set; }
    public string? Kpp { get; set; }
    public string? Ogrn { get; set; }

    public string? LegalAddress { get; set; }
    public string? ActualAddress { get; set; }

    public string? BankName { get; set; }
    public string? Bik { get; set; }
    public string? AccountNumber { get; set; }
    public string? CorrAccount { get; set; }

    public bool VatPayer { get; set; }

    public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
}

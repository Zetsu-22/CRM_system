namespace CrmSystem.Domain;

public sealed class Warehouse : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public required string Name { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

namespace CrmSystem.Domain;

/// <summary>
/// Подразделение/филиал внутри Company.
/// Пользователям назначаются организации — они видят только их данные (если не IsCompanyOwner).
/// </summary>
public sealed class Organization : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public required string Name { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

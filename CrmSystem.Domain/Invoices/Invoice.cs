namespace CrmSystem.Domain;

public sealed class Invoice : AuditableEntity, ITenantOwned
{
    public string? Number { get; set; }
    public DateOnly Date { get; set; }

    public Guid CompanyId { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid CounterpartyId { get; set; }

    public Guid? OrderId { get; set; }

    public InvoiceStatus Status { get; set; }

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public decimal PaidAmount { get; set; }

    public string? Comment { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();

    // Phase 4.3 — снапшоты реквизитов и расчёта
    public string? BankRequisitesSnapshotJson { get; set; }
    public string? CounterpartyInnSnapshot { get; set; }
    public string? CounterpartyKppSnapshot { get; set; }
    public string? DeliveryAddressSnapshotJson { get; set; }
    public string? CalculationSnapshotJson { get; set; }
}

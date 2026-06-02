namespace CrmSystem.Domain;

public sealed class SalesOrder : AuditableEntity, ITenantOwned
{
    public string? Number { get; set; }
    public DateOnly Date { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public bool IsAssemblyChecklistClosed { get; set; }

    public string? Comment { get; set; }

    public Guid? PickerId { get; set; }
    public Guid? DriverId { get; set; }
    public string? BankAccount { get; set; }
    public string? Contract { get; set; }
    public string? DeliveryAddress { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();

    // Phase 4.1 — история переходов статусов
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    // Phase 4.2 — снапшоты реквизитов на момент подтверждения
    public string? BankAccountSnapshotJson { get; set; }
    public string? DeliveryAddressSnapshotJson { get; set; }

    // Phase 4.4 — чек-лист сборки
    public string? PickingChecklistJson { get; set; }
}

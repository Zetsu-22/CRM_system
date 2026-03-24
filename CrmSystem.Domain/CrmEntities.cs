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

public sealed class Counterparty : AuditableEntity
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

public sealed class CounterpartyContact : AuditableEntity
{
    public Guid CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public sealed class Item : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public ItemType Type { get; set; }
    public required string Sku { get; set; }
    public string? Barcode { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public required string Unit { get; set; }
    public VatRate VatRate { get; set; }

    public bool IsStockItem { get; set; }
}

public sealed class Warehouse : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public required string Name { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class StockBalance : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public decimal QtyOnHand { get; set; }
    public decimal QtyReserved { get; set; }
}

public enum StockMovementSourceType
{
    Order = 1,
    Upd = 2,
    Manual = 3
}

public sealed class StockMovement : AuditableEntity
{
    public DateTimeOffset Date { get; set; }

    public Guid WarehouseId { get; set; }
    public Guid ItemId { get; set; }

    public decimal QtyDeltaOnHand { get; set; }
    public decimal QtyDeltaReserved { get; set; }

    public StockMovementSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
}

public sealed class SalesOrder : AuditableEntity
{
    public string? Number { get; set; }
    public DateOnly Date { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public bool IsAssemblyChecklistClosed { get; set; }

    public string? Comment { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}

public enum DiscountType
{
    None = 0,
    Percent = 1,
    Amount = 2
}

public sealed class SalesOrderLine : AuditableEntity
{
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public required string NameSnapshot { get; set; }
    public VatRate VatRateSnapshot { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal Gross { get; set; }

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}

public sealed class Invoice : AuditableEntity
{
    public string? Number { get; set; }
    public DateOnly Date { get; set; }

    public Guid CompanyId { get; set; }
    public Guid CounterpartyId { get; set; }

    public Guid? OrderId { get; set; }

    public InvoiceStatus Status { get; set; }

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public decimal PaidAmount { get; set; }

    public string? Comment { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

public sealed class InvoiceLine : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid ItemId { get; set; }

    public required string NameSnapshot { get; set; }
    public VatRate VatRateSnapshot { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal Gross { get; set; }
}

public sealed class Upd : AuditableEntity
{
    public string? Number { get; set; }
    public DateOnly Date { get; set; }

    public Guid CompanyId { get; set; }
    public Guid CounterpartyId { get; set; }

    public Guid? OrderId { get; set; }
    public Guid? InvoiceId { get; set; }

    public UpdFunction Function { get; set; }
    public UpdStatus Status { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public string? Comment { get; set; }

    public ICollection<UpdLine> Lines { get; set; } = new List<UpdLine>();
}

public sealed class UpdLine : AuditableEntity
{
    public Guid UpdId { get; set; }
    public Upd? Upd { get; set; }

    public Guid ItemId { get; set; }

    public required string NameSnapshot { get; set; }
    public VatRate VatRateSnapshot { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal Gross { get; set; }

    public Guid? WarehouseId { get; set; }
}

public sealed class Payment : AuditableEntity
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

public sealed class PaymentAllocation : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal Amount { get; set; }
}

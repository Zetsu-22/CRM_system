namespace CrmSystem.Domain;

public abstract class Entity
{
    public Guid Id { get; set; }
}

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public uint RowVersion { get; set; }
}

public enum CounterpartyType
{
    Customer = 1,
    Supplier = 2,
    Both = 3
}

public enum CounterpartyStatus
{
    Active = 1,
    Blocked = 2
}

public enum ItemType
{
    Product = 1,
    Service = 2
}

public enum VatRate
{
    Vat0 = 0,
    Vat5 = 5,
    Vat7 = 7,
    Vat10 = 10,
    Vat22 = 22
}

public enum OrderStatus
{
    Draft = 1,
    Confirmed = 2,
    Closed = 3,
    Cancelled = 4
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Closed = 3,
    Cancelled = 4
}

public enum UpdFunction
{
    ShippingAndInvoice = 1,
    ShippingOnly = 2,
    InvoiceOnly = 3
}

public enum UpdStatus
{
    Draft = 1,
    Registered = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentType
{
    Incoming = 1,
    Outgoing = 2
}

public enum PaymentMethod
{
    Bank = 1,
    Cash = 2,
    Card = 3
}

public enum PaymentStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}

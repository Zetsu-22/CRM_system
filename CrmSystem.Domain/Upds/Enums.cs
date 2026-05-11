namespace CrmSystem.Domain;

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

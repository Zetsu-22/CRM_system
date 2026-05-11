namespace CrmSystem.Domain;

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

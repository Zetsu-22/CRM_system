namespace CrmSystem.Domain;

public enum OrderStatus
{
    Draft = 0,
    Processing = 1,
    WaitingForPayment = 2,
    BillConfirmed = 3,
    Manufacturing = 4,
    Picking = 5,
    TransferredToTransport = 6,
    DeliveringByTransport = 7,
    Delivering = 8,
    AwaitingPickup = 9,
    Completed = 10,
    Cancelled = 11
}

public enum DiscountType
{
    None = 0,
    Percent = 1,
    Amount = 2
}

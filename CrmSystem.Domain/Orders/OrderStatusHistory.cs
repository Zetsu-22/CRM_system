namespace CrmSystem.Domain;

public sealed class OrderStatusHistory : AuditableEntity
{
    public Guid OrderId { get; set; }
    public SalesOrder? Order { get; set; }

    public OrderStatus Status { get; set; }

    /// <summary>Момент перехода (UTC).</summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>UserId пользователя, выполнившего переход.</summary>
    public Guid? ChangedBy { get; set; }

    /// <summary>Комментарий к переходу (необязательный).</summary>
    public string? Comment { get; set; }
}

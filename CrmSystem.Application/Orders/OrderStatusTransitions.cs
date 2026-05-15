using CrmSystem.Application.Orders.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Orders;

/// <summary>
/// State-machine переходов статуса заказа. Чистая логика — без I/O.
/// Перенесено из <c>OrdersController.GetNextStatus</c>.
/// </summary>
public static class OrderStatusTransitions
{
    /// <summary>Финальные статусы — переход невозможен.</summary>
    public static bool IsFinal(OrderStatus s) =>
        s == OrderStatus.Completed || s == OrderStatus.Cancelled;

    /// <summary>Статусы, в которых заказ нельзя редактировать (уже передан в доставку).</summary>
    public static bool IsLocked(OrderStatus s) =>
        s == OrderStatus.DeliveringByTransport ||
        s == OrderStatus.Delivering ||
        s == OrderStatus.AwaitingPickup ||
        s == OrderStatus.Completed ||
        s == OrderStatus.Cancelled;

    /// <summary>
    /// Возвращает следующий статус с учётом типа доставки и наличия дефицита по складу.
    /// Возвращает null, если перехода нет.
    /// </summary>
    public static OrderStatus? GetNextStatus(OrderStatus current, string? deliveryType, bool needsManufacturing)
    {
        // Финальные статусы — переход невозможен.
        if (IsFinal(current)) return null;

        // selfPickup: после Picking → AwaitingPickup (пропускаем транспортные и Delivering)
        if (deliveryType == DeliveryTypes.SelfPickup && current == OrderStatus.Picking)
            return OrderStatus.AwaitingPickup;

        // companyDelivery: после Picking → Delivering (пропускаем транспортные)
        if (deliveryType == DeliveryTypes.CompanyDelivery && current == OrderStatus.Picking)
            return OrderStatus.Delivering;

        // transportCompany: Picking → TransferredToTransport → DeliveringByTransport → AwaitingPickup
        if (deliveryType == DeliveryTypes.TransportCompany && current == OrderStatus.DeliveringByTransport)
            return OrderStatus.AwaitingPickup;

        // Базовый линейный переход по порядку enum
        var next = (OrderStatus)((int)current + 1);
        if (!Enum.IsDefined(typeof(OrderStatus), next)) return null;

        // Manufacturing появляется только при дефиците
        if (next == OrderStatus.Manufacturing && !needsManufacturing)
            return OrderStatus.Picking;

        // Пропускаем Delivering при transportCompany
        if (deliveryType == DeliveryTypes.TransportCompany && next == OrderStatus.Delivering)
            return OrderStatus.AwaitingPickup;

        // Пропускаем транспортные статусы при selfPickup/companyDelivery
        if ((deliveryType == DeliveryTypes.SelfPickup || deliveryType == DeliveryTypes.CompanyDelivery) &&
            (next == OrderStatus.TransferredToTransport || next == OrderStatus.DeliveringByTransport))
            return GetNextStatus(next, deliveryType, needsManufacturing);

        return next;
    }
}

using CrmSystem.Application.Common;
using CrmSystem.Application.Orders.Dtos;

namespace CrmSystem.Application.Orders;

public interface IOrdersService
{
    Task<Result<IReadOnlyList<OrderResponse>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);

    // Phase 6.11 — поиск и фильтрация
    Task<Result<IReadOnlyList<OrderResponse>>> SearchAsync(IReadOnlyCollection<Guid>? organizationIds, OrderFilterRequest filter, CancellationToken ct);
    Task<Result<PagedResponse<OrderResponse>>> GetPagedAsync(PageRequest request, IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<OrderResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<OrderResponse>> CreateAsync(SalesOrderDto dto, CancellationToken ct);
    Task<Result<OrderResponse>> UpdateAsync(Guid id, SalesOrderDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<OrderStatusResponse>> ConfirmAsync(Guid id, CancellationToken ct);
    Task<Result<OrderStatusResponse>> CancelAsync(Guid id, CancellationToken ct);
    Task<Result<OrderStatusResponse>> NextStatusAsync(Guid id, string? deliveryType, CancellationToken ct);
    Task<Result<AssemblyChecklistResponse>> CloseAssemblyChecklistAsync(Guid id, CancellationToken ct);

    // Phase 4.1 — история статусов
    Task<Result<IReadOnlyList<OrderStatusHistoryEntry>>> GetStatusHistoryAsync(Guid id, CancellationToken ct);

    // Phase 4.4 — чек-лист сборки
    Task<Result<PickingChecklistResponse>> GetPickingChecklistAsync(Guid id, CancellationToken ct);
    Task<Result<PickingChecklistResponse>> UpdatePickingChecklistAsync(Guid id, List<PickingChecklistItem> items, CancellationToken ct);
    Task<Result<PickingChecklistResponse>> MarkPickingItemDoneAsync(Guid id, Guid itemId, Guid? doneBy, CancellationToken ct);
}

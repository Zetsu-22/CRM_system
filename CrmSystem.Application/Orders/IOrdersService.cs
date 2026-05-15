using CrmSystem.Application.Common;
using CrmSystem.Application.Orders.Dtos;

namespace CrmSystem.Application.Orders;

public interface IOrdersService
{
    Task<Result<IReadOnlyList<OrderResponse>>> GetAllAsync(CancellationToken ct);
    Task<Result<OrderResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<OrderResponse>> CreateAsync(SalesOrderDto dto, CancellationToken ct);
    Task<Result<OrderResponse>> UpdateAsync(Guid id, SalesOrderDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<OrderStatusResponse>> ConfirmAsync(Guid id, CancellationToken ct);
    Task<Result<OrderStatusResponse>> CancelAsync(Guid id, CancellationToken ct);
    Task<Result<OrderStatusResponse>> NextStatusAsync(Guid id, string? deliveryType, CancellationToken ct);
    Task<Result<AssemblyChecklistResponse>> CloseAssemblyChecklistAsync(Guid id, CancellationToken ct);
}

using CrmSystem.Application.Common;
using CrmSystem.Application.Items.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Items;

public interface IItemsService
{
    Task<Result<IReadOnlyList<Item>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<PagedResponse<Item>>> GetPagedAsync(PageRequest request, IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<Item>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Item>> CreateAsync(ItemDto dto, CancellationToken ct);
    Task<Result<Item>> UpdateAsync(Guid id, ItemDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}

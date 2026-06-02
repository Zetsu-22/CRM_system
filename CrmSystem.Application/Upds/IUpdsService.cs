using CrmSystem.Application.Common;
using CrmSystem.Application.Upds.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Upds;

public interface IUpdsService
{
    Task<Result<IReadOnlyList<Upd>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<Upd>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Upd>> CreateAsync(UpdDto dto, CancellationToken ct);
    Task<Result<Upd>> UpdateAsync(Guid id, UpdDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}

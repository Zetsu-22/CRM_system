using CrmSystem.Application.Common;
using CrmSystem.Application.Counterparties.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Counterparties;

public interface ICounterpartiesService
{
    Task<Result<IReadOnlyList<Counterparty>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<PagedResponse<Counterparty>>> GetPagedAsync(PageRequest request, IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<Counterparty>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Counterparty>> CreateAsync(CounterpartyDto dto, CancellationToken ct);
    Task<Result<Counterparty>> UpdateAsync(Guid id, CounterpartyDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}

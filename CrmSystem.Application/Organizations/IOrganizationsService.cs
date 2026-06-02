using CrmSystem.Application.Common;
using CrmSystem.Application.Organizations.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Organizations;

public interface IOrganizationsService
{
    Task<Result<IReadOnlyList<Organization>>> GetAllAsync(CancellationToken ct);
    Task<Result<Organization>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Organization>> CreateAsync(OrganizationDto dto, CancellationToken ct);
    Task<Result<Organization>> UpdateAsync(Guid id, OrganizationDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}

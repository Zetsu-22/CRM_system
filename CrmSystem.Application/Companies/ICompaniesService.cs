using CrmSystem.Application.Common;
using CrmSystem.Application.Companies.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Companies;

public interface ICompaniesService
{
    Task<Result<IReadOnlyList<Company>>> GetAllAsync(CancellationToken ct);
    Task<Result<Company>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Company>> GetMineAsync(CancellationToken ct);
    Task<Result<Company>> UpdateAsync(Guid id, CompanyDto dto, CancellationToken ct);
}

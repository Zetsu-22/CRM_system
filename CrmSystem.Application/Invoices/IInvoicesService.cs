using CrmSystem.Application.Common;
using CrmSystem.Application.Invoices.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Invoices;

public interface IInvoicesService
{
    Task<Result<IReadOnlyList<Invoice>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<Invoice>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Invoice>> CreateAsync(InvoiceDto dto, CancellationToken ct);
    Task<Result<Invoice>> UpdateAsync(Guid id, InvoiceDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}

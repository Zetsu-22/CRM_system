using CrmSystem.Application.Common;
using CrmSystem.Application.Payments.Dtos;
using CrmSystem.Domain;

namespace CrmSystem.Application.Payments;

public interface IPaymentsService
{
    Task<Result<IReadOnlyList<Payment>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct);
    Task<Result<Payment>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<Payment>> CreateAsync(PaymentDto dto, CancellationToken ct);
    Task<Result<Payment>> UpdateAsync(Guid id, PaymentDto dto, CancellationToken ct);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}

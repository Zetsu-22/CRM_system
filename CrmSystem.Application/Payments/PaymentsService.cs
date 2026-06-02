using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Payments.Dtos;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Payments;

public sealed class PaymentsService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : IPaymentsService
{
    public async Task<Result<IReadOnlyList<Payment>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<Payment>>.Success(Array.Empty<Payment>());

        var query = db.Payments
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Allocations)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        var list = await query.OrderByDescending(x => x.Date).ToListAsync(ct);
        return Result<IReadOnlyList<Payment>>.Success(list);
    }

    public async Task<Result<Payment>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await db.Payments
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);

        return entity is null ? Error.NotFound() : Result<Payment>.Success(entity);
    }

    public async Task<Result<Payment>> CreateAsync(PaymentDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var now = clock.UtcNow;
        var entity = new Payment
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            OrganizationId = dto.OrganizationId,
            CounterpartyId = dto.CounterpartyId,
            Date = dto.Date,
            Type = dto.Type,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            BankReference = dto.BankReference,
            ExternalId = dto.ExternalId,
            Status = dto.Status,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Payments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<Payment>> UpdateAsync(Guid id, PaymentDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var entity = await db.Payments
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.Type = dto.Type;
        entity.OrganizationId = dto.OrganizationId;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.Date = dto.Date;
        entity.Amount = dto.Amount;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.BankReference = dto.BankReference;
        entity.ExternalId = dto.ExternalId;
        entity.Status = dto.Status;
        entity.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Error.Forbidden();

        var entity = await db.Payments
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

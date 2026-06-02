using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Counterparties.Dtos;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Counterparties;

public sealed class CounterpartiesService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : ICounterpartiesService
{
    public async Task<Result<IReadOnlyList<Counterparty>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessContractors) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<Counterparty>>.Success(Array.Empty<Counterparty>());

        var query = db.Counterparties
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Addresses)
            .Include(x => x.Contacts)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        var list = await query.OrderBy(x => x.Name).ToListAsync(ct);
        return Result<IReadOnlyList<Counterparty>>.Success(list);
    }

    public async Task<Result<PagedResponse<Counterparty>>> GetPagedAsync(PageRequest request, IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessContractors) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<PagedResponse<Counterparty>>.Success(
            new PagedResponse<Counterparty>(Array.Empty<Counterparty>(), 1, request.Take, 0, 0));

        var query = db.Counterparties
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Addresses)
            .Include(x => x.Contacts)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        query = (request.SortBy?.ToLowerInvariant()) switch
        {
            "inn"  => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Inn)  : query.OrderBy(x => x.Inn),
            "type" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Type) : query.OrderBy(x => x.Type),
            _      => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
        };

        var paged = await query.ToPagedResponseAsync(request, ct);
        return Result<PagedResponse<Counterparty>>.Success(paged);
    }

    public async Task<Result<Counterparty>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessContractors) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await db.Counterparties
            .Include(x => x.Addresses)
            .Include(x => x.Contacts)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);

        return entity is null ? Error.NotFound() : Result<Counterparty>.Success(entity);
    }

    public async Task<Result<Counterparty>> CreateAsync(CounterpartyDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessContractorsEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var now = clock.UtcNow;
        var entity = new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            OrganizationId = dto.OrganizationId,
            Name = dto.Name,
            FullName = dto.FullName,
            Type = dto.Type,
            Status = dto.Status,
            Inn = dto.Inn,
            Kpp = dto.Kpp,
            Ogrn = dto.Ogrn,
            PaymentTermsDays = dto.PaymentTermsDays,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Counterparties.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<Counterparty>> UpdateAsync(Guid id, CounterpartyDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessContractorsEdit) return Error.Forbidden();

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var entity = await db.Counterparties
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.OrganizationId = dto.OrganizationId;
        entity.Name = dto.Name;
        entity.FullName = dto.FullName;
        entity.Type = dto.Type;
        entity.Status = dto.Status;
        entity.Inn = dto.Inn;
        entity.Kpp = dto.Kpp;
        entity.Ogrn = dto.Ogrn;
        entity.PaymentTermsDays = dto.PaymentTermsDays;
        entity.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessContractorsEdit) return Error.Forbidden();

        var entity = await db.Counterparties
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

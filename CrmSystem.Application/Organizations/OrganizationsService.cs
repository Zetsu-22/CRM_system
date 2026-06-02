using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Organizations.Dtos;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Organizations;

public sealed class OrganizationsService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : IOrganizationsService
{
    public async Task<Result<IReadOnlyList<Organization>>> GetAllAsync(CancellationToken ct)
    {
        if (cu.CompanyId is null) return Result<IReadOnlyList<Organization>>.Success(Array.Empty<Organization>());

        var list = await db.Organizations
            .Where(x => x.CompanyId == cu.CompanyId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Organization>>.Success(list);
    }

    public async Task<Result<Organization>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await db.Organizations
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        return entity is null ? Error.NotFound() : Result<Organization>.Success(entity);
    }

    public async Task<Result<Organization>> CreateAsync(OrganizationDto dto, CancellationToken ct)
    {
        if (!cu.IsCompanyOwner) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var now = clock.UtcNow;
        var entity = new Organization
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            Name = dto.Name,
            Address = dto.Address,
            IsDefault = dto.IsDefault,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (dto.IsDefault)
        {
            var existing = await db.Organizations
                .Where(x => x.CompanyId == cu.CompanyId && x.IsDefault)
                .ToListAsync(ct);
            foreach (var o in existing) o.IsDefault = false;
        }

        db.Organizations.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<Organization>> UpdateAsync(Guid id, OrganizationDto dto, CancellationToken ct)
    {
        if (!cu.IsCompanyOwner) return Error.Forbidden();

        var entity = await db.Organizations
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (dto.IsDefault && !entity.IsDefault)
        {
            var existing = await db.Organizations
                .Where(x => x.CompanyId == cu.CompanyId && x.IsDefault)
                .ToListAsync(ct);
            foreach (var o in existing) o.IsDefault = false;
        }

        entity.Name = dto.Name;
        entity.Address = dto.Address;
        entity.IsDefault = dto.IsDefault;
        entity.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsCompanyOwner) return Error.Forbidden();

        var entity = await db.Organizations
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

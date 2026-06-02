using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Items.Dtos;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Items;

public sealed class ItemsService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : IItemsService
{
    public async Task<Result<IReadOnlyList<Item>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclature) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<Item>>.Success(Array.Empty<Item>());

        var query = db.Items
            .Where(x => x.CompanyId == cu.CompanyId)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        var list = await query.OrderBy(x => x.Name).ToListAsync(ct);
        return Result<IReadOnlyList<Item>>.Success(list);
    }

    public async Task<Result<PagedResponse<Item>>> GetPagedAsync(PageRequest request, IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclature) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<PagedResponse<Item>>.Success(
            new PagedResponse<Item>(Array.Empty<Item>(), 1, request.Take, 0, 0));

        var query = db.Items
            .Where(x => x.CompanyId == cu.CompanyId)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        query = (request.SortBy?.ToLowerInvariant()) switch
        {
            "sku"  => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Sku)  : query.OrderBy(x => x.Sku),
            "type" => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Type) : query.OrderBy(x => x.Type),
            _      => request.SortDirection == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
        };

        var paged = await query.ToPagedResponseAsync(request, ct);
        return Result<PagedResponse<Item>>.Success(paged);
    }

    public async Task<Result<Item>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclature) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await db.Items
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);

        return entity is null ? Error.NotFound() : Result<Item>.Success(entity);
    }

    public async Task<Result<Item>> CreateAsync(ItemDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclatureEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var now = clock.UtcNow;
        var entity = new Item
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            OrganizationId = dto.OrganizationId,
            Type = dto.Type,
            Sku = dto.Sku?.Trim() is { Length: > 0 } s ? s : "ITEM-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Barcode = dto.Barcode,
            Name = dto.Name,
            Description = dto.Description,
            Unit = dto.Unit,
            VatRate = dto.VatRate,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Items.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<Item>> UpdateAsync(Guid id, ItemDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclatureEdit) return Error.Forbidden();

        var orgOk = await db.Organizations.AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!orgOk) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var entity = await db.Items
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.OrganizationId = dto.OrganizationId;
        entity.Type = dto.Type;
        entity.Barcode = dto.Barcode;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Unit = dto.Unit;
        entity.VatRate = dto.VatRate;
        entity.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclatureEdit) return Error.Forbidden();

        var entity = await db.Items
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

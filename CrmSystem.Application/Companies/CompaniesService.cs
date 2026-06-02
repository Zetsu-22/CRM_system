using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Companies.Dtos;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Companies;

public sealed class CompaniesService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : ICompaniesService
{
    public async Task<Result<IReadOnlyList<Company>>> GetAllAsync(CancellationToken ct)
    {
        var list = await db.Companies
            .Where(x => x.Id == cu.CompanyId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return Result<IReadOnlyList<Company>>.Success(list);
    }

    public async Task<Result<Company>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (id != cu.CompanyId) return Error.Forbidden();

        var entity = await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? Error.NotFound() : Result<Company>.Success(entity);
    }

    public async Task<Result<Company>> GetMineAsync(CancellationToken ct)
    {
        if (cu.CompanyId is null) return Error.NotFound("Пользователь не привязан к компании");

        var entity = await db.Companies.FirstOrDefaultAsync(x => x.Id == cu.CompanyId, ct);
        return entity is null ? Error.NotFound() : Result<Company>.Success(entity);
    }

    public async Task<Result<Company>> UpdateAsync(Guid id, CompanyDto dto, CancellationToken ct)
    {
        if (!cu.IsCompanyOwner) return Error.Forbidden();
        if (id != cu.CompanyId) return Error.Forbidden();

        var entity = await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Error.NotFound();

        entity.Name = dto.Name;
        entity.Inn = dto.Inn;
        entity.Kpp = dto.Kpp;
        entity.Ogrn = dto.Ogrn;
        entity.LegalAddress = dto.LegalAddress;
        entity.ActualAddress = dto.ActualAddress;
        entity.BankName = dto.BankName;
        entity.Bik = dto.Bik;
        entity.AccountNumber = dto.AccountNumber;
        entity.CorrAccount = dto.CorrAccount;
        entity.VatPayer = dto.VatPayer;
        entity.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return entity;
    }
}

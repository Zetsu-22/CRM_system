using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
[Authorize]
public sealed class CompaniesController(CrmDbContext db, CurrentUserService cu) : ControllerBase
{
    // GET /companies — возвращает компанию текущего пользователя
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var cid = cu.CompanyId;
        var list = await db.Companies
            .Where(x => x.Id == cid)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (id != cu.CompanyId) return Forbid();
        var item = await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    // GET /companies/mine — удобный алиас для получения своей компании
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (cu.CompanyId is null) return NotFound(new { error = "Пользователь не привязан к компании" });
        var item = await db.Companies.FirstOrDefaultAsync(x => x.Id == cu.CompanyId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    // PUT /companies/{id} — только владелец может обновить реквизиты компании
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompanyDto dto, CancellationToken ct)
    {
        if (!cu.IsCompanyOwner) return Forbid();
        if (id != cu.CompanyId) return Forbid();

        var entity = await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

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
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }
}

public sealed record CompanyDto(
    string Name,
    string? Inn,
    string? Kpp,
    string? Ogrn,
    string? LegalAddress,
    string? ActualAddress,
    string? BankName,
    string? Bik,
    string? AccountNumber,
    string? CorrAccount,
    bool VatPayer);

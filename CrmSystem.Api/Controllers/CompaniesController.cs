using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
public sealed class CompaniesController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await db.Companies.OrderBy(x => x.Name).ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyDto dto, CancellationToken ct)
    {
        var entity = new Company
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Inn = dto.Inn,
            Kpp = dto.Kpp,
            Ogrn = dto.Ogrn,
            LegalAddress = dto.LegalAddress,
            ActualAddress = dto.ActualAddress,
            BankName = dto.BankName,
            Bik = dto.Bik,
            AccountNumber = dto.AccountNumber,
            CorrAccount = dto.CorrAccount,
            VatPayer = dto.VatPayer,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Companies.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompanyDto dto, CancellationToken ct)
    {
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
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

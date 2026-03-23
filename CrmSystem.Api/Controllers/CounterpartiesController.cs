using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/counterparties")]
[Authorize]
public sealed class CounterpartiesController(CrmDbContext db, CurrentUserService cu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (cu.CompanyId is null) return Ok(Array.Empty<object>());
        if (!cu.IsAccessContractors) return Forbid();
        var cid = cu.CompanyId;
        var list = await db.Counterparties
            .Where(x => x.CompanyId == cid)
            .Include(x => x.Addresses)
            .Include(x => x.Contacts)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return NotFound();
        if (!cu.IsAccessContractors) return Forbid();
        var item = await db.Counterparties
            .Include(x => x.Addresses)
            .Include(x => x.Contacts)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CounterpartyDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessContractorsEdit) return Forbid();
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });
        var entity = new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            Name = dto.Name,
            FullName = dto.FullName,
            Type = dto.Type,
            Status = dto.Status,
            Inn = dto.Inn,
            Kpp = dto.Kpp,
            Ogrn = dto.Ogrn,
            PaymentTermsDays = dto.PaymentTermsDays,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Counterparties.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CounterpartyDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessContractorsEdit) return Forbid();
        var entity = await db.Counterparties.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();

        entity.Name = dto.Name;
        entity.FullName = dto.FullName;
        entity.Type = dto.Type;
        entity.Status = dto.Status;
        entity.Inn = dto.Inn;
        entity.Kpp = dto.Kpp;
        entity.Ogrn = dto.Ogrn;
        entity.PaymentTermsDays = dto.PaymentTermsDays;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessContractorsEdit) return Forbid();
        var entity = await db.Counterparties.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record CounterpartyDto(
    string Name,
    string? FullName,
    CounterpartyType Type,
    CounterpartyStatus Status,
    string? Inn,
    string? Kpp,
    string? Ogrn,
    int? PaymentTermsDays);

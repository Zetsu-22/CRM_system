using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController(CrmDbContext db, CurrentUserService cu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (cu.CompanyId is null) return Ok(Array.Empty<object>());
        if (!cu.IsAccessFinancialJournal) return Forbid();
        var list = await db.Payments
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Allocations)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return NotFound();
        if (!cu.IsAccessFinancialJournal) return Forbid();
        var item = await db.Payments
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Forbid();
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });
        var entity = new Payment
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            CompanyId = cu.CompanyId.Value,
            CounterpartyId = dto.CounterpartyId,
            Date = dto.Date,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            BankReference = dto.BankReference,
            ExternalId = dto.ExternalId,
            Status = dto.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Payments.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PaymentDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Forbid();
        var entity = await db.Payments.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();

        entity.Type = dto.Type;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.Date = dto.Date;
        entity.Amount = dto.Amount;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.BankReference = dto.BankReference;
        entity.ExternalId = dto.ExternalId;
        entity.Status = dto.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournal) return Forbid();
        var entity = await db.Payments.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record PaymentDto(
    PaymentType Type,
    Guid? CounterpartyId,
    DateOnly Date,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? BankReference,
    string? ExternalId,
    PaymentStatus Status);

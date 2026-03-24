using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/upds")]
[Authorize]
public sealed class UpdsController(CrmDbContext db, CurrentUserService cu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (cu.CompanyId is null) return Ok(Array.Empty<object>());
        if (!cu.IsAccessFinancialJournal) return Forbid();
        var list = await db.Upds
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return NotFound();
        if (!cu.IsAccessFinancialJournal) return Forbid();
        var item = await db.Upds
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpdDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Forbid();
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });
        var entity = new Upd
        {
            Id = Guid.NewGuid(),
            Number = dto.Number,
            Date = dto.Date,
            CompanyId = cu.CompanyId.Value,
            CounterpartyId = dto.CounterpartyId,
            OrderId = dto.OrderId,
            InvoiceId = dto.InvoiceId,
            Function = dto.Function,
            Status = dto.Status,
            ShippingDate = dto.ShippingDate,
            Comment = dto.Comment,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var line in dto.Lines)
        {
            var net = line.Qty * line.UnitPrice;
            var vatRate = (int)line.VatRateSnapshot / 100m;
            var vat = net * vatRate;
            entity.Lines.Add(new UpdLine
            {
                Id = Guid.NewGuid(),
                ItemId = line.ItemId,
                NameSnapshot = line.NameSnapshot,
                VatRateSnapshot = line.VatRateSnapshot,
                Qty = line.Qty,
                UnitPrice = line.UnitPrice,
                DiscountType = line.DiscountType,
                DiscountValue = line.DiscountValue,
                Net = net,
                Vat = vat,
                Gross = net + vat,
                WarehouseId = line.WarehouseId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        entity.TotalNet = entity.Lines.Sum(l => l.Net);
        entity.TotalVat = entity.Lines.Sum(l => l.Vat);
        entity.TotalGross = entity.Lines.Sum(l => l.Gross);

        db.Upds.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Forbid();
        var entity = await db.Upds
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();

        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.OrderId = dto.OrderId;
        entity.InvoiceId = dto.InvoiceId;
        entity.Function = dto.Function;
        entity.Status = dto.Status;
        entity.ShippingDate = dto.ShippingDate;
        entity.Comment = dto.Comment;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var l in entity.Lines) l.IsDeleted = true;
        entity.Lines.Clear();

        foreach (var line in dto.Lines)
        {
            var net = line.Qty * line.UnitPrice;
            var vatRate = (int)line.VatRateSnapshot / 100m;
            var vat = net * vatRate;
            entity.Lines.Add(new UpdLine
            {
                Id = Guid.NewGuid(),
                ItemId = line.ItemId,
                NameSnapshot = line.NameSnapshot,
                VatRateSnapshot = line.VatRateSnapshot,
                Qty = line.Qty,
                UnitPrice = line.UnitPrice,
                DiscountType = line.DiscountType,
                DiscountValue = line.DiscountValue,
                Net = net,
                Vat = vat,
                Gross = net + vat,
                WarehouseId = line.WarehouseId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        entity.TotalNet = entity.Lines.Sum(l => l.Net);
        entity.TotalVat = entity.Lines.Sum(l => l.Vat);
        entity.TotalGross = entity.Lines.Sum(l => l.Gross);

        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessFinancialJournalEdit) return Forbid();
        var entity = await db.Upds.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record UpdDto(
    string? Number,
    DateOnly Date,
    Guid CounterpartyId,
    Guid? OrderId,
    Guid? InvoiceId,
    UpdFunction Function,
    UpdStatus Status,
    DateOnly? ShippingDate,
    string? Comment,
    List<UpdLineDto> Lines);

public sealed record UpdLineDto(
    Guid ItemId,
    string NameSnapshot,
    VatRate VatRateSnapshot,
    decimal Qty,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue,
    Guid? WarehouseId);

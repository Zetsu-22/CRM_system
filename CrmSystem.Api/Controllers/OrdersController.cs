using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await db.Orders
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Orders
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalesOrderDto dto, CancellationToken ct)
    {
        var entity = new SalesOrder
        {
            Id = Guid.NewGuid(),
            Number = dto.Number,
            Date = dto.Date,
            CompanyId = dto.CompanyId,
            CounterpartyId = dto.CounterpartyId,
            Status = dto.Status,
            Comment = dto.Comment,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var line in dto.Lines)
        {
            var net = line.Qty * line.UnitPrice;
            var vatRate = (int)line.VatRateSnapshot / 100m;
            var vat = net * vatRate;
            entity.Lines.Add(new SalesOrderLine
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

        db.Orders.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SalesOrderDto dto, CancellationToken ct)
    {
        var entity = await db.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.CompanyId = dto.CompanyId;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.Status = dto.Status;
        entity.Comment = dto.Comment;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var l in entity.Lines)
        {
            l.IsDeleted = true;
        }

        entity.Lines.Clear();
        foreach (var line in dto.Lines)
        {
            var net = line.Qty * line.UnitPrice;
            var vatRate = (int)line.VatRateSnapshot / 100m;
            var vat = net * vatRate;
            entity.Lines.Add(new SalesOrderLine
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

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] StatusDto dto, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.Status = dto.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record SalesOrderDto(
    string? Number,
    DateOnly Date,
    Guid CompanyId,
    Guid CounterpartyId,
    OrderStatus Status,
    string? Comment,
    List<SalesOrderLineDto> Lines);

public sealed record SalesOrderLineDto(
    Guid ItemId,
    string NameSnapshot,
    VatRate VatRateSnapshot,
    decimal Qty,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue,
    Guid? WarehouseId);

public sealed record StatusDto(OrderStatus Status);

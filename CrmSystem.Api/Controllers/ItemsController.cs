using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/items")]
public sealed class ItemsController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await db.Items.OrderBy(x => x.Name).ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ItemDto dto, CancellationToken ct)
    {
        var entity = new Item
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Sku = "ITEM-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Barcode = dto.Barcode,
            Name = dto.Name,
            Description = dto.Description,
            Unit = dto.Unit,
            VatRate = dto.VatRate,
            IsStockItem = dto.IsStockItem,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Items.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ItemDto dto, CancellationToken ct)
    {
        var entity = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        entity.Type = dto.Type;
        entity.Barcode = dto.Barcode;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Unit = dto.Unit;
        entity.VatRate = dto.VatRate;
        entity.IsStockItem = dto.IsStockItem;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record ItemDto(
    ItemType Type,
    string? Barcode,
    string Name,
    string? Description,
    string Unit,
    VatRate VatRate,
    bool IsStockItem);

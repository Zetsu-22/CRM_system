using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/warehouses")]
public sealed class WarehousesController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await db.Warehouses.OrderBy(x => x.Name).ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarehouseDto dto, CancellationToken ct)
    {
        var entity = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Address = dto.Address,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Warehouses.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WarehouseDto dto, CancellationToken ct)
    {
        var entity = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        entity.Name = dto.Name;
        entity.Address = dto.Address;
        entity.IsDefault = dto.IsDefault;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(CancellationToken ct)
    {
        var list = await db.StockBalances
            .Include(x => x.Warehouse)
            .Include(x => x.Item)
            .OrderBy(x => x.Warehouse!.Name)
            .ThenBy(x => x.Item!.Name)
            .ToListAsync(ct);
        return Ok(list);
    }
}

public sealed record WarehouseDto(string Name, string? Address, bool IsDefault);

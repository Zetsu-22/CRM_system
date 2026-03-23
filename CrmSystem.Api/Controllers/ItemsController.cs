using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/items")]
[Authorize]
public sealed class ItemsController(CrmDbContext db, CurrentUserService cu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (cu.CompanyId is null) return Ok(Array.Empty<object>());
        if (!cu.IsAccessNomenclature) return Forbid();
        var list = await db.Items
            .Where(x => x.CompanyId == cu.CompanyId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return NotFound();
        if (!cu.IsAccessNomenclature) return Forbid();
        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ItemDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessNomenclatureEdit) return Forbid();
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });
        var entity = new Item
        {
            Id = Guid.NewGuid(),
            CompanyId = cu.CompanyId.Value,
            Type = dto.Type,
            Sku = dto.Sku?.Trim() is { Length: > 0 } s ? s : "ITEM-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
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
        if (!cu.IsAccessNomenclatureEdit) return Forbid();
        var entity = await db.Items.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
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
        if (!cu.IsAccessNomenclatureEdit) return Forbid();
        var entity = await db.Items.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record ItemDto(
    ItemType Type,
    string? Sku,
    string? Barcode,
    string Name,
    string? Description,
    string Unit,
    VatRate VatRate,
    bool IsStockItem);

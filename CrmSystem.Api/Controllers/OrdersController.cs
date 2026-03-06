using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(CrmDbContext db) : ControllerBase
{
    static OrderResponse ToResponse(SalesOrder o) => new(
        o.Id, o.Number, o.Date, o.CompanyId, o.Company?.Name,
        o.CounterpartyId, o.Counterparty?.Name,
        o.Status, o.Comment, o.TotalNet, o.TotalVat, o.TotalGross,
        o.CreatedAt, o.UpdatedAt,
        o.Lines.Select(l => new OrderLineResponse(
            l.Id, l.ItemId, l.Item?.Name, l.NameSnapshot, l.VatRateSnapshot,
            l.Qty, l.UnitPrice, l.DiscountType, l.DiscountValue,
            l.Net, l.Vat, l.Gross, l.WarehouseId)).ToList());

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await db.Orders
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);
        return Ok(list.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.Orders
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(ToResponse(item));
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
            Status = OrderStatus.Draft,
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
        await db.Entry(entity).Reference(x => x.Company).LoadAsync(ct);
        await db.Entry(entity).Reference(x => x.Counterparty).LoadAsync(ct);
        foreach (var l in entity.Lines) await db.Entry(l).Reference(x => x.Item).LoadAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SalesOrderDto dto, CancellationToken ct)
    {
        var entity = await db.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        if (entity.Status != OrderStatus.Draft)
            return Conflict(new { error = new { code = "invalid_state", message = $"Нельзя редактировать заказ в статусе '{entity.Status}'. Редактирование доступно только в статусе Draft." } });

        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.CompanyId = dto.CompanyId;
        entity.CounterpartyId = dto.CounterpartyId;
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
        await db.Entry(entity).Reference(x => x.Company).LoadAsync(ct);
        await db.Entry(entity).Reference(x => x.Counterparty).LoadAsync(ct);
        foreach (var l in entity.Lines) await db.Entry(l).Reference(x => x.Item).LoadAsync(ct);
        return Ok(ToResponse(entity));
    }

    // POST /orders/{id}/confirm  — Draft → Confirmed
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != OrderStatus.Draft)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Переход Confirm возможен только из статуса Draft. Текущий статус: {entity.Status}." } });

        entity.Status = OrderStatus.Confirmed;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
    }

    // POST /orders/{id}/fulfill  — Confirmed → Fulfilled
    [HttpPost("{id:guid}/fulfill")]
    public async Task<IActionResult> Fulfill(Guid id, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != OrderStatus.Confirmed)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Переход Fulfill возможен только из статуса Confirmed. Текущий статус: {entity.Status}." } });

        entity.Status = OrderStatus.Fulfilled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
    }

    // POST /orders/{id}/close  — Confirmed или Fulfilled → Closed
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != OrderStatus.Confirmed && entity.Status != OrderStatus.Fulfilled)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Переход Close возможен только из статусов Confirmed или Fulfilled. Текущий статус: {entity.Status}." } });

        entity.Status = OrderStatus.Closed;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
    }

    // POST /orders/{id}/cancel  — Draft или Confirmed → Cancelled
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != OrderStatus.Draft && entity.Status != OrderStatus.Confirmed)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Отмена возможна только из статусов Draft или Confirmed. Текущий статус: {entity.Status}." } });

        entity.Status = OrderStatus.Cancelled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
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

public sealed record OrderLineResponse(
    Guid Id, Guid ItemId, string? ItemName, string NameSnapshot, VatRate VatRateSnapshot,
    decimal Qty, decimal UnitPrice, DiscountType DiscountType, decimal DiscountValue,
    decimal Net, decimal Vat, decimal Gross, Guid? WarehouseId);

public sealed record OrderResponse(
    Guid Id, string? Number, DateOnly Date,
    Guid CompanyId, string? CompanyName,
    Guid CounterpartyId, string? CounterpartyName,
    OrderStatus Status, string? Comment,
    decimal TotalNet, decimal TotalVat, decimal TotalGross,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    List<OrderLineResponse> Lines);

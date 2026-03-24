using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public sealed class OrdersController(CrmDbContext db, CurrentUserService cu) : ControllerBase
{
    static OrderResponse ToResponse(SalesOrder o) => new(
        o.Id, o.Number, o.Date, o.CompanyId, o.Company?.Name,
        o.CounterpartyId, o.Counterparty?.Name,
        o.Status, o.Comment, o.TotalNet, o.TotalVat, o.TotalGross,
        o.IsAssemblyChecklistClosed,
        o.CreatedAt, o.UpdatedAt,
        o.Lines.Select(l => new OrderLineResponse(
            l.Id, l.ItemId, l.Item?.Name, l.NameSnapshot, l.VatRateSnapshot,
            l.Qty, l.UnitPrice, l.DiscountType, l.DiscountValue,
            l.Net, l.Vat, l.Gross, l.WarehouseId)).ToList());

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Forbid();
        if (cu.CompanyId is null) return Ok(Array.Empty<object>());
        var list = await db.Orders
            .Where(x => x.CompanyId == cu.CompanyId)
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
        if (!cu.CanViewOrders) return Forbid();
        if (cu.CompanyId is null) return NotFound();
        var item = await db.Orders
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalesOrderDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Forbid();
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });
        var entity = new SalesOrder
        {
            Id = Guid.NewGuid(),
            Number = dto.Number,
            Date = dto.Date,
            CompanyId = cu.CompanyId.Value,
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
        if (!cu.IsAccessOrdersEdit) return Forbid();
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });
        var entity = await db.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();

        if (IsLocked(entity.Status))
            return Conflict(new { error = new { code = "invalid_state", message = $"Нельзя редактировать заказ в статусе '{entity.Status}'. Редактирование заблокировано после передачи в доставку." } });

        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.Comment = dto.Comment;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var l in entity.Lines)
        {
            l.IsDeleted = true;
            l.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var now = DateTimeOffset.UtcNow;
        var newLines = new List<SalesOrderLine>(dto.Lines.Count);
        foreach (var line in dto.Lines)
        {
            var net = line.Qty * line.UnitPrice;
            var vatRate = (int)line.VatRateSnapshot / 100m;
            var vat = net * vatRate;
            var newLine = new SalesOrderLine
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
                CreatedAt = now,
                UpdatedAt = now,
            };
            newLines.Add(newLine);
            entity.Lines.Add(newLine);
        }

        entity.TotalNet = newLines.Sum(l => l.Net);
        entity.TotalVat = newLines.Sum(l => l.Vat);
        entity.TotalGross = newLines.Sum(l => l.Gross);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = new { code = "concurrency_conflict", message = "Заказ был изменён или удалён. Обновите страницу и повторите попытку." } });
        }

        var updated = await db.Orders
            .Where(x => x.Id == id && x.CompanyId == cu.CompanyId)
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(ct);

        return updated is null ? NotFound() : Ok(ToResponse(updated));
    }

    // Статусы, после которых заказ нельзя редактировать (уже в доставке)
    static bool IsLocked(OrderStatus s) =>
        s == OrderStatus.DeliveringByTransport ||
        s == OrderStatus.Delivering ||
        s == OrderStatus.AwaitingPickup ||
        s == OrderStatus.Completed ||
        s == OrderStatus.Cancelled;

    // POST /orders/{id}/confirm  — Draft → Processing
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Forbid();
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();
        if (entity.Status != OrderStatus.Draft)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Подтверждение возможно только из статуса Draft. Текущий статус: {entity.Status}." } });

        entity.Status = OrderStatus.Processing;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
    }

    // POST /orders/{id}/cancel  — любой не-финальный → Cancelled
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Forbid();
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();
        if (entity.Status == OrderStatus.Completed || entity.Status == OrderStatus.Cancelled)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Нельзя отменить заказ в финальном статусе '{entity.Status}'." } });

        entity.Status = OrderStatus.Cancelled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
    }

    // POST /orders/{id}/next-status?deliveryType=selfPickup|companyDelivery|transportCompany
    // Переводит заказ на следующий статус с учётом типа доставки
    [HttpPost("{id:guid}/next-status")]
    public async Task<IActionResult> NextStatus(Guid id, [FromQuery] string? deliveryType, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Forbid();
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();

        if (entity.Status == OrderStatus.Completed || entity.Status == OrderStatus.Cancelled)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Заказ в финальном статусе '{entity.Status}', переход невозможен." } });

        var next = GetNextStatus(entity.Status, deliveryType);
        if (next is null)
            return Conflict(new { error = new { code = "invalid_transition", message = $"Нет следующего статуса для '{entity.Status}'." } });

        entity.Status = next.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, status = entity.Status });
    }

    static OrderStatus? GetNextStatus(OrderStatus current, string? deliveryType)
    {
        // selfPickup: после Picking → AwaitingPickup (пропускаем транспортные и Delivering)
        if (deliveryType == "selfPickup" && current == OrderStatus.Picking)
            return OrderStatus.AwaitingPickup;

        // companyDelivery: после Picking → Delivering (пропускаем транспортные)
        if (deliveryType == "companyDelivery" && current == OrderStatus.Picking)
            return OrderStatus.Delivering;

        // transportCompany: Picking → TransferredToTransport → DeliveringByTransport → AwaitingPickup
        // Delivering не используется при транспортной компании
        if (deliveryType == "transportCompany" && current == OrderStatus.DeliveringByTransport)
            return OrderStatus.AwaitingPickup;

        // Базовый линейный переход по порядку enum (0..11)
        var next = (OrderStatus)((int)current + 1);
        if (!Enum.IsDefined(typeof(OrderStatus), next)) return null;

        // Пропускаем Delivering при transportCompany
        if (deliveryType == "transportCompany" && next == OrderStatus.Delivering)
            return OrderStatus.AwaitingPickup;

        // Пропускаем транспортные статусы при selfPickup/companyDelivery
        if ((deliveryType == "selfPickup" || deliveryType == "companyDelivery") &&
            (next == OrderStatus.TransferredToTransport || next == OrderStatus.DeliveringByTransport))
            return GetNextStatus(next, deliveryType); // рекурсивно пропускаем

        return next;
    }

    // POST /orders/{id}/assembly-checklist/close
    // Закрывает чеклист сборки. Сборщик может закрывать только в статусе Picking.
    [HttpPost("{id:guid}/assembly-checklist/close")]
    public async Task<IActionResult> CloseAssemblyChecklist(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return BadRequest(new { error = "Пользователь не привязан к компании" });

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return NotFound();

        var canClose = cu.IsAccessOrdersEdit || (cu.IsOrderPicker && entity.Status == OrderStatus.Picking);
        if (!canClose) return Forbid();

        if (entity.IsAssemblyChecklistClosed)
            return Ok(new { id = entity.Id, isAssemblyChecklistClosed = true });

        entity.IsAssemblyChecklistClosed = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id, isAssemblyChecklistClosed = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Forbid();
        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
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
    bool IsAssemblyChecklistClosed,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    List<OrderLineResponse> Lines);

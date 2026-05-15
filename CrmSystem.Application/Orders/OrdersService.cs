using CrmSystem.Application.Abstractions;
using CrmSystem.Application.Common;
using CrmSystem.Application.Orders.Dtos;
using CrmSystem.Application.Orders.Mappers;
using CrmSystem.Application.Orders.Pricing;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Orders;

public sealed class OrdersService(CrmDbContext db, ICurrentUser cu, IDateTimeProvider clock) : IOrdersService
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> GetAllAsync(CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<OrderResponse>>.Success(Array.Empty<OrderResponse>());

        var list = await db.Orders
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);

        var itemIds = list.SelectMany(x => x.Lines).Select(l => l.ItemId).Distinct().ToList();
        var available = await GetAvailableQtyByItemId(cu.CompanyId.Value, itemIds, ct);

        IReadOnlyList<OrderResponse> result = list.Select(x => OrderMapper.ToResponse(x, available)).ToList();
        return Result<IReadOnlyList<OrderResponse>>.Success(result);
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await LoadFullAsync(id, ct);
        if (entity is null) return Error.NotFound();

        var available = await GetAvailableQtyByItemId(cu.CompanyId.Value, entity.Lines.Select(l => l.ItemId).Distinct().ToList(), ct);
        return OrderMapper.ToResponse(entity, available);
    }

    public async Task<Result<OrderResponse>> CreateAsync(SalesOrderDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var now = clock.UtcNow;
        var entity = new SalesOrder
        {
            Id = Guid.NewGuid(),
            Number = dto.Number,
            Date = dto.Date,
            CompanyId = cu.CompanyId.Value,
            CounterpartyId = dto.CounterpartyId,
            Status = OrderStatus.Draft,
            Comment = dto.Comment,
            PickerId = dto.PickerId,
            DriverId = dto.DriverId,
            BankAccount = dto.BankAccount,
            Contract = dto.Contract,
            DeliveryAddress = dto.DeliveryAddress,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var line in dto.Lines)
            entity.Lines.Add(BuildLine(line, now));

        ApplyTotals(entity);

        db.Orders.Add(entity);
        await db.SaveChangesAsync(ct);

        // Дозагружаем навигации для маппинга
        await db.Entry(entity).Reference(x => x.Company).LoadAsync(ct);
        await db.Entry(entity).Reference(x => x.Counterparty).LoadAsync(ct);
        foreach (var l in entity.Lines) await db.Entry(l).Reference(x => x.Item).LoadAsync(ct);

        var available = await GetAvailableQtyByItemId(cu.CompanyId.Value, entity.Lines.Select(l => l.ItemId).Distinct().ToList(), ct);
        return OrderMapper.ToResponse(entity, available);
    }

    public async Task<Result<OrderResponse>> UpdateAsync(Guid id, SalesOrderDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (OrderStatusTransitions.IsLocked(entity.Status))
            return Error.InvalidState($"Нельзя редактировать заказ в статусе '{entity.Status}'. Редактирование заблокировано после передачи в доставку.");

        if (dto.RowVersion.HasValue && dto.RowVersion.Value != entity.RowVersion)
        {
            return Error.ConcurrencyConflict(
                "Заказ был изменён или удалён. Обновите страницу и повторите попытку.",
                new { reason = "row_version_mismatch", expectedRowVersion = entity.RowVersion, requestRowVersion = dto.RowVersion });
        }

        var now = clock.UtcNow;
        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.CounterpartyId = dto.CounterpartyId;
        entity.Comment = dto.Comment;
        entity.PickerId = dto.PickerId;
        entity.DriverId = dto.DriverId;
        entity.BankAccount = dto.BankAccount;
        entity.Contract = dto.Contract;
        entity.DeliveryAddress = dto.DeliveryAddress;
        entity.UpdatedAt = now;

        // Hard-delete всех старых строк (минуя change tracker, включая soft-deleted).
        // См. docs/known-issues.md ORD-002.
        await db.OrderLines
            .IgnoreQueryFilters()
            .Where(x => x.OrderId == id)
            .ExecuteDeleteAsync(ct);

        var newLines = new List<SalesOrderLine>(dto.Lines.Count);
        foreach (var line in dto.Lines)
        {
            var newLine = BuildLine(line, now);
            newLine.OrderId = entity.Id;
            newLines.Add(newLine);
            db.OrderLines.Add(newLine);
        }

        entity.TotalNet   = newLines.Sum(l => l.Net);
        entity.TotalVat   = newLines.Sum(l => l.Vat);
        entity.TotalGross = newLines.Sum(l => l.Gross);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entries = ex.Entries.Select(e => new
            {
                entity = e.Metadata.Name,
                state = e.State.ToString(),
                keys = e.Properties.Where(p => p.Metadata.IsPrimaryKey())
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
            }).ToList();

            return Error.ConcurrencyConflict(
                "Заказ был изменён или удалён. Обновите страницу и повторите попытку.",
                new { reason = "db_update_concurrency_exception", entries });
        }

        var updated = await LoadFullAsync(id, ct);
        if (updated is null) return Error.NotFound();

        var available = await GetAvailableQtyByItemId(cu.CompanyId.Value, updated.Lines.Select(l => l.ItemId).Distinct().ToList(), ct);
        return OrderMapper.ToResponse(updated, available);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.IsDeleted = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<OrderStatusResponse>> ConfirmAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (entity.Status != OrderStatus.Draft)
            return Error.InvalidTransition($"Подтверждение возможно только из статуса Draft. Текущий статус: {entity.Status}.");

        entity.Status = OrderStatus.Processing;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return new OrderStatusResponse(entity.Id, entity.Status);
    }

    public async Task<Result<OrderStatusResponse>> CancelAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (OrderStatusTransitions.IsFinal(entity.Status))
            return Error.InvalidTransition($"Нельзя отменить заказ в финальном статусе '{entity.Status}'.");

        entity.Status = OrderStatus.Cancelled;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return new OrderStatusResponse(entity.Id, entity.Status);
    }

    public async Task<Result<OrderStatusResponse>> NextStatusAsync(Guid id, string? deliveryType, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var entity = await db.Orders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (OrderStatusTransitions.IsFinal(entity.Status))
            return Error.InvalidTransition($"Заказ в финальном статусе '{entity.Status}', переход невозможен.");

        var itemIds = entity.Lines.Select(l => l.ItemId).Distinct().ToList();
        var available = await GetAvailableQtyByItemId(cu.CompanyId.Value, itemIds, ct);
        var needsManufacturing = NeedsManufacturing(entity, available);

        var next = OrderStatusTransitions.GetNextStatus(entity.Status, deliveryType, needsManufacturing);
        if (next is null)
            return Error.InvalidTransition($"Нет следующего статуса для '{entity.Status}'.");

        entity.Status = next.Value;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return new OrderStatusResponse(entity.Id, entity.Status);
    }

    public async Task<Result<AssemblyChecklistResponse>> CloseAssemblyChecklistAsync(Guid id, CancellationToken ct)
    {
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        var canClose = cu.IsAccessOrdersEdit || (cu.IsOrderPicker && entity.Status == OrderStatus.Picking);
        if (!canClose) return Error.Forbidden();

        if (entity.IsAssemblyChecklistClosed)
            return new AssemblyChecklistResponse(entity.Id, true);

        entity.IsAssemblyChecklistClosed = true;
        entity.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return new AssemblyChecklistResponse(entity.Id, true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Task<SalesOrder?> LoadFullAsync(Guid id, CancellationToken ct) =>
        db.Orders
            .Include(x => x.Company)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);

    private SalesOrderLine BuildLine(SalesOrderLineDto line, DateTimeOffset now)
    {
        var totals = OrderTotalsCalculator.CalculateLine(line.Qty, line.UnitPrice, line.VatRateSnapshot);
        return new SalesOrderLine
        {
            Id = Guid.NewGuid(),
            ItemId = line.ItemId,
            NameSnapshot = line.NameSnapshot,
            VatRateSnapshot = line.VatRateSnapshot,
            Qty = line.Qty,
            UnitPrice = line.UnitPrice,
            DiscountType = line.DiscountType,
            DiscountValue = line.DiscountValue,
            Net = totals.Net,
            Vat = totals.Vat,
            Gross = totals.Gross,
            WarehouseId = line.WarehouseId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static void ApplyTotals(SalesOrder o)
    {
        var totals = OrderTotalsCalculator.CalculateOrder(
            o.Lines.Select(l => new LineTotals(l.Net, l.Vat, l.Gross)));
        o.TotalNet   = totals.TotalNet;
        o.TotalVat   = totals.TotalVat;
        o.TotalGross = totals.TotalGross;
    }

    private static bool NeedsManufacturing(SalesOrder o, IReadOnlyDictionary<Guid, decimal> availableByItemId)
    {
        foreach (var l in o.Lines)
        {
            availableByItemId.TryGetValue(l.ItemId, out var available);
            if (available < l.Qty) return true;
        }
        return false;
    }

    private async Task<Dictionary<Guid, decimal>> GetAvailableQtyByItemId(
        Guid companyId, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
    {
        if (itemIds.Count == 0) return new Dictionary<Guid, decimal>();

        var rows = await db.StockBalances
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Where(x => itemIds.Contains(x.ItemId) && x.Warehouse != null && x.Warehouse.CompanyId == companyId)
            .Select(x => new { x.ItemId, Available = x.QtyOnHand - x.QtyReserved })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Available));
    }
}

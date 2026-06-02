using System.Text.Json;
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
    public async Task<Result<IReadOnlyList<OrderResponse>>> GetAllAsync(IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<OrderResponse>>.Success(Array.Empty<OrderResponse>());

        var query = db.Orders
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Company)
            .Include(x => x.Organization)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        var list = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);

        IReadOnlyList<OrderResponse> result = list.Select(OrderMapper.ToResponse).ToList();
        return Result<IReadOnlyList<OrderResponse>>.Success(result);
    }

    // ── Phase 6.11 — поиск и фильтрация ────────────────────────────────────

    public async Task<Result<IReadOnlyList<OrderResponse>>> SearchAsync(
        IReadOnlyCollection<Guid>? organizationIds,
        OrderFilterRequest filter,
        CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<OrderResponse>>.Success(Array.Empty<OrderResponse>());

        var query = db.Orders
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Company)
            .Include(x => x.Organization)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .AsQueryable();

        // Фильтр по организациям
        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        // Полнотекстовый поиск (по номеру, имени контрагента, комментарию)
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var q = filter.SearchQuery.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Number ?? "", $"%{q}%") ||
                EF.Functions.ILike(x.Counterparty!.Name, $"%{q}%") ||
                EF.Functions.ILike(x.Comment ?? "", $"%{q}%"));
        }

        // Фильтр по статусам
        if (filter.Statuses is { Count: > 0 })
            query = query.Where(x => filter.Statuses.Contains(x.Status));

        // Фильтр по контрагенту
        if (filter.CounterpartyId is not null)
            query = query.Where(x => x.CounterpartyId == filter.CounterpartyId);

        // Фильтр по диапазону дат
        if (filter.DateFrom is not null)
            query = query.Where(x => x.Date >= filter.DateFrom.Value);
        if (filter.DateTo is not null)
            query = query.Where(x => x.Date <= filter.DateTo.Value);

        // Фильтр по сумме
        if (filter.TotalFrom is not null)
            query = query.Where(x => x.TotalGross >= filter.TotalFrom.Value);
        if (filter.TotalTo is not null)
            query = query.Where(x => x.TotalGross <= filter.TotalTo.Value);

        var list = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);

        IReadOnlyList<OrderResponse> result = list.Select(OrderMapper.ToResponse).ToList();
        return Result<IReadOnlyList<OrderResponse>>.Success(result);
    }

    public async Task<Result<PagedResponse<OrderResponse>>> GetPagedAsync(PageRequest request, IReadOnlyCollection<Guid>? organizationIds, CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<PagedResponse<OrderResponse>>.Success(
            new PagedResponse<OrderResponse>(Array.Empty<OrderResponse>(), 1, request.Take, 0, 0));

        var query = db.Orders
            .Where(x => x.CompanyId == cu.CompanyId)
            .Include(x => x.Company)
            .Include(x => x.Organization)
            .Include(x => x.Counterparty)
            .Include(x => x.Lines).ThenInclude(l => l.Item)
            .AsQueryable();

        if (organizationIds is { Count: > 0 })
            query = query.Where(x => organizationIds.Contains(x.OrganizationId));

        query = (request.SortBy?.ToLowerInvariant()) switch
        {
            "number" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Number)
                : query.OrderBy(x => x.Number),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),
            _ => query.OrderByDescending(x => x.Date),
        };

        var paged = await query.ToPagedResponseAsync(request, ct);
        var mapped = new PagedResponse<OrderResponse>(
            paged.Items.Select(OrderMapper.ToResponse).ToList(),
            paged.Page, paged.PageSize, paged.TotalCount, paged.TotalPages);
        return Result<PagedResponse<OrderResponse>>.Success(mapped);
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.NotFound();

        var entity = await LoadFullAsync(id, ct);
        if (entity is null) return Error.NotFound();

        return OrderMapper.ToResponse(entity);
    }

    public async Task<Result<OrderResponse>> CreateAsync(SalesOrderDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var organizationExists = await db.Organizations
            .AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!organizationExists) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

        var now = clock.UtcNow;
        var entity = new SalesOrder
        {
            Id = Guid.NewGuid(),
            Number = dto.Number,
            Date = dto.Date,
            CompanyId = cu.CompanyId.Value,
            OrganizationId = dto.OrganizationId,
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
        await db.Entry(entity).Reference(x => x.Organization).LoadAsync(ct);
        await db.Entry(entity).Reference(x => x.Counterparty).LoadAsync(ct);
        foreach (var l in entity.Lines) await db.Entry(l).Reference(x => x.Item).LoadAsync(ct);

        return OrderMapper.ToResponse(entity);
    }

    public async Task<Result<OrderResponse>> UpdateAsync(Guid id, SalesOrderDto dto, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var organizationExists = await db.Organizations
            .AsNoTracking()
            .AnyAsync(x => x.Id == dto.OrganizationId && x.CompanyId == cu.CompanyId, ct);
        if (!organizationExists) return Error.Validation("Выбранная организация не найдена или не принадлежит текущей компании");

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
        entity.OrganizationId = dto.OrganizationId;
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

        return OrderMapper.ToResponse(updated);
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

    // ── Статусные переходы ──────────────────────────────────────────────────

    public async Task<Result<OrderStatusResponse>> ConfirmAsync(Guid id, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (entity.Status != OrderStatus.Draft)
            return Error.InvalidTransition($"Подтверждение возможно только из статуса Draft. Текущий статус: {entity.Status}.");

        var now = clock.UtcNow;

        // Phase 4.2 — снапшот реквизитов при подтверждении
        entity.BankAccountSnapshotJson = entity.BankAccount is not null
            ? JsonSerializer.Serialize(new { bankAccount = entity.BankAccount, contract = entity.Contract })
            : null;
        entity.DeliveryAddressSnapshotJson = entity.DeliveryAddress is not null
            ? JsonSerializer.Serialize(new { deliveryAddress = entity.DeliveryAddress })
            : null;

        entity.Status = OrderStatus.Processing;
        entity.UpdatedAt = now;

        // Phase 4.1 — запись в историю
        AppendHistory(entity, OrderStatus.Processing, now, "Заказ подтверждён");

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

        var now = clock.UtcNow;
        entity.Status = OrderStatus.Cancelled;
        entity.UpdatedAt = now;

        // Phase 4.1
        AppendHistory(entity, OrderStatus.Cancelled, now, "Заказ отменён");

        await db.SaveChangesAsync(ct);
        return new OrderStatusResponse(entity.Id, entity.Status);
    }

    public async Task<Result<OrderStatusResponse>> NextStatusAsync(Guid id, string? deliveryType, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var entity = await db.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        if (OrderStatusTransitions.IsFinal(entity.Status))
            return Error.InvalidTransition($"Заказ в финальном статусе '{entity.Status}', переход невозможен.");

        // Phase 4.5 — guard: WaitingForPayment → BillConfirmed требует Invoice
        if (entity.Status == OrderStatus.WaitingForPayment)
        {
            var hasIssuedInvoice = await db.Invoices.AsNoTracking()
                .AnyAsync(x => x.OrderId == id && x.Status == InvoiceStatus.Issued && !x.IsDeleted, ct);
            if (!hasIssuedInvoice)
                return new Error("no_issued_invoice", "Невозможно подтвердить оплату: нет выставленного счёта для заказа.");
        }

        // Phase 4.5 — guard: Picking → ReadyForShipment требует закрытого чек-листа
        var needsManufacturing = false;
        var next = OrderStatusTransitions.GetNextStatus(entity.Status, deliveryType, needsManufacturing);
        if (next is null)
            return Error.InvalidTransition($"Нет следующего статуса для '{entity.Status}'.");

        if (entity.Status == OrderStatus.Picking &&
            (next == OrderStatus.AwaitingPickup || next == OrderStatus.Delivering || next == OrderStatus.TransferredToTransport))
        {
            if (!entity.IsAssemblyChecklistClosed)
            {
                var items = PickingChecklistHelper.Deserialize(entity.PickingChecklistJson);
                if (items.Count > 0 && items.Any(x => !x.Done))
                    return new Error("checklist_not_closed", "Невозможно перейти в следующий статус: не все позиции чек-листа сборки отмечены.");
            }
        }

        var now = clock.UtcNow;

        // Phase 4.4 — при переходе в Picking генерируем чек-лист
        if (next == OrderStatus.Picking && entity.PickingChecklistJson is null)
        {
            var checklistItems = entity.Lines.Select(l => new PickingChecklistItem(
                l.ItemId, l.NameSnapshot, l.Qty, false, null, null)).ToList();
            entity.PickingChecklistJson = PickingChecklistHelper.Serialize(checklistItems);
        }

        entity.Status = next.Value;
        entity.UpdatedAt = now;

        // Phase 4.1
        var historyComment = next.Value switch
        {
            OrderStatus.WaitingForPayment       => "Ожидает оплаты",
            OrderStatus.BillConfirmed           => "Счёт подтверждён",
            OrderStatus.Manufacturing           => "Передана в производство",
            OrderStatus.Picking                 => "Передана на сборку",
            OrderStatus.TransferredToTransport  => deliveryType is not null ? $"Передан в ТК ({deliveryType})" : "Передан в транспортную компанию",
            OrderStatus.DeliveringByTransport   => "Доставляется транспортной компанией",
            OrderStatus.Delivering              => "Передан курьеру",
            OrderStatus.AwaitingPickup          => "Ожидает самовывоза",
            OrderStatus.Completed               => "Заказ выполнен",
            _                                   => "Смена статуса"
        };
        AppendHistory(entity, next.Value, now, historyComment);

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

    // ── Phase 4.1 — история статусов ───────────────────────────────────────

    public async Task<Result<IReadOnlyList<OrderStatusHistoryEntry>>> GetStatusHistoryAsync(Guid id, CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();
        if (cu.CompanyId is null) return Result<IReadOnlyList<OrderStatusHistoryEntry>>.Success(Array.Empty<OrderStatusHistoryEntry>());

        var orderExists = await db.Orders.AsNoTracking()
            .AnyAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (!orderExists) return Error.NotFound();

        var history = await db.OrderStatusHistory
            .Where(x => x.OrderId == id)
            .OrderBy(x => x.ChangedAt)
            .ToListAsync(ct);

        IReadOnlyList<OrderStatusHistoryEntry> result = history.Select(h => new OrderStatusHistoryEntry(
            h.Id, h.Status, h.Status.ToString(), h.ChangedAt, h.ChangedBy, h.Comment)).ToList();

        return Result<IReadOnlyList<OrderStatusHistoryEntry>>.Success(result);
    }

    // ── Phase 4.4 — чек-лист сборки ────────────────────────────────────────

    public async Task<Result<PickingChecklistResponse>> GetPickingChecklistAsync(Guid id, CancellationToken ct)
    {
        if (!cu.CanViewOrders) return Error.Forbidden();

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        var items = PickingChecklistHelper.Deserialize(entity.PickingChecklistJson);
        return PickingChecklistHelper.ToResponse(id, items);
    }

    public async Task<Result<PickingChecklistResponse>> UpdatePickingChecklistAsync(Guid id, List<PickingChecklistItem> items, CancellationToken ct)
    {
        if (!cu.IsAccessOrdersEdit) return Error.Forbidden();

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        entity.PickingChecklistJson = PickingChecklistHelper.Serialize(items);
        entity.IsAssemblyChecklistClosed = items.Count > 0 && items.All(x => x.Done);
        entity.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return PickingChecklistHelper.ToResponse(id, items);
    }

    public async Task<Result<PickingChecklistResponse>> MarkPickingItemDoneAsync(Guid id, Guid itemId, Guid? doneBy, CancellationToken ct)
    {
        if (cu.CompanyId is null) return Error.Validation("Пользователь не привязан к компании");

        var entity = await db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == cu.CompanyId, ct);
        if (entity is null) return Error.NotFound();

        var canMark = cu.IsAccessOrdersEdit || (cu.IsOrderPicker && entity.Status == OrderStatus.Picking);
        if (!canMark) return Error.Forbidden();

        var items = PickingChecklistHelper.Deserialize(entity.PickingChecklistJson);
        var item = items.FirstOrDefault(x => x.ItemId == itemId);
        if (item is null) return Error.NotFound();

        var now = clock.UtcNow;
        var updated = item with { Done = true, DoneBy = doneBy ?? cu.UserId, DoneAt = now };
        var index = items.IndexOf(item);
        items[index] = updated;

        entity.PickingChecklistJson = PickingChecklistHelper.Serialize(items);

        // Автозакрытие при полном чек-листе
        if (items.All(x => x.Done))
        {
            entity.IsAssemblyChecklistClosed = true;
        }

        entity.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return PickingChecklistHelper.ToResponse(id, items);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Task<SalesOrder?> LoadFullAsync(Guid id, CancellationToken ct) =>
        db.Orders
            .Include(x => x.Company)
            .Include(x => x.Organization)
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

    /// <summary>Добавляет запись в StatusHistory без SaveChanges.</summary>
    private void AppendHistory(SalesOrder entity, OrderStatus status, DateTimeOffset changedAt, string? comment)
    {
        var entry = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = entity.Id,
            Status = status,
            ChangedAt = changedAt,
            ChangedBy = cu.UserId,
            Comment = comment,
            CreatedAt = changedAt,
            UpdatedAt = changedAt,
        };
        db.OrderStatusHistory.Add(entry);
    }
}

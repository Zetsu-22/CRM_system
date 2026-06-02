using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Common;
using CrmSystem.Application.Orders;
using CrmSystem.Application.Orders.Dtos;
using CrmSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickingChecklistItem = CrmSystem.Application.Orders.Dtos.PickingChecklistItem;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public sealed class OrdersController(IOrdersService orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? organizationIds,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        // Phase 6.11 — search & filter params
        [FromQuery] string? search,
        [FromQuery] string? statuses,
        [FromQuery] Guid? counterpartyId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] decimal? totalFrom,
        [FromQuery] decimal? totalTo,
        CancellationToken ct)
    {
        var parsed = OrgIdParser.Parse(organizationIds);

        // Build filter — only when at least one filter param is provided
        var statusList = ParseStatuses(statuses);
        var parsedDateFrom = string.IsNullOrWhiteSpace(dateFrom) ? (DateOnly?)null : DateOnly.Parse(dateFrom);
        var parsedDateTo   = string.IsNullOrWhiteSpace(dateTo)   ? (DateOnly?)null : DateOnly.Parse(dateTo);

        var filter = new OrderFilterRequest(search, statusList, counterpartyId, parsedDateFrom, parsedDateTo, totalFrom, totalTo);
        bool hasFilter = !filter.IsEmpty;

        if (page.HasValue)
        {
            var req = new PageRequest(page.Value, pageSize ?? 20, sortBy, sortDirection ?? "asc");
            return (await orders.GetPagedAsync(req, parsed, ct)).ToActionResult(this);
        }

        if (hasFilter)
            return (await orders.SearchAsync(parsed, filter, ct)).ToActionResult(this);

        return (await orders.GetAllAsync(parsed, ct)).ToActionResult(this);
    }

    private static IReadOnlyList<OrderStatus>? ParseStatuses(string? statuses)
    {
        if (string.IsNullOrWhiteSpace(statuses)) return null;
        var result = statuses.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Enum.TryParse<OrderStatus>(s.Trim(), true, out var v) ? (OrderStatus?)v : null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();
        return result.Count > 0 ? result : null;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await orders.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalesOrderDto dto, CancellationToken ct) =>
        (await orders.CreateAsync(dto, ct))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SalesOrderDto dto, CancellationToken ct) =>
        (await orders.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await orders.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }

    // POST /orders/{id}/confirm  — Draft → Processing
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct) =>
        (await orders.ConfirmAsync(id, ct)).ToActionResult(this);

    // POST /orders/{id}/cancel
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        (await orders.CancelAsync(id, ct)).ToActionResult(this);

    // POST /orders/{id}/next-status?deliveryType=selfPickup|companyDelivery|transportCompany
    [HttpPost("{id:guid}/next-status")]
    public async Task<IActionResult> NextStatus(Guid id, [FromQuery] string? deliveryType, CancellationToken ct) =>
        (await orders.NextStatusAsync(id, deliveryType, ct)).ToActionResult(this);

    // POST /orders/{id}/assembly-checklist/close
    [HttpPost("{id:guid}/assembly-checklist/close")]
    public async Task<IActionResult> CloseAssemblyChecklist(Guid id, CancellationToken ct) =>
        (await orders.CloseAssemblyChecklistAsync(id, ct)).ToActionResult(this);

    // GET /orders/{id}/status-history  [4.1]
    [HttpGet("{id:guid}/status-history")]
    public async Task<IActionResult> GetStatusHistory(Guid id, CancellationToken ct) =>
        (await orders.GetStatusHistoryAsync(id, ct)).ToActionResult(this);

    // GET /orders/{id}/picking-checklist  [4.4]
    [HttpGet("{id:guid}/picking-checklist")]
    public async Task<IActionResult> GetPickingChecklist(Guid id, CancellationToken ct) =>
        (await orders.GetPickingChecklistAsync(id, ct)).ToActionResult(this);

    // POST /orders/{id}/picking-checklist  [4.4]
    [HttpPost("{id:guid}/picking-checklist")]
    public async Task<IActionResult> UpdatePickingChecklist(
        Guid id, [FromBody] List<PickingChecklistItem> items, CancellationToken ct) =>
        (await orders.UpdatePickingChecklistAsync(id, items, ct)).ToActionResult(this);

    // POST /orders/{id}/picking-checklist/items/{itemId}/done  [4.4]
    [HttpPost("{id:guid}/picking-checklist/items/{itemId:guid}/done")]
    public async Task<IActionResult> MarkPickingItemDone(
        Guid id, Guid itemId, [FromBody] MarkPickingItemDoneRequest? req, CancellationToken ct) =>
        (await orders.MarkPickingItemDoneAsync(id, itemId, req?.DoneBy, ct)).ToActionResult(this);
}

using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Orders;
using CrmSystem.Application.Orders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public sealed class OrdersController(IOrdersService orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        (await orders.GetAllAsync(ct)).ToActionResult(this);

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
}

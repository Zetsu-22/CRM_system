using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Payments;
using CrmSystem.Application.Payments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController(IPaymentsService payments) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? organizationIds, CancellationToken ct) =>
        (await payments.GetAllAsync(OrgIdParser.Parse(organizationIds), ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await payments.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PaymentDto dto, CancellationToken ct) =>
        (await payments.CreateAsync(dto, ct))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PaymentDto dto, CancellationToken ct) =>
        (await payments.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await payments.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

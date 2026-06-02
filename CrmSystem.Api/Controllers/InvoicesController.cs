using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Invoices;
using CrmSystem.Application.Invoices.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public sealed class InvoicesController(IInvoicesService invoices) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? organizationIds, CancellationToken ct) =>
        (await invoices.GetAllAsync(OrgIdParser.Parse(organizationIds), ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await invoices.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InvoiceDto dto, CancellationToken ct) =>
        (await invoices.CreateAsync(dto, ct))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InvoiceDto dto, CancellationToken ct) =>
        (await invoices.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await invoices.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

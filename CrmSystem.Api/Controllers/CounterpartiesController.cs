using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Common;
using CrmSystem.Application.Counterparties;
using CrmSystem.Application.Counterparties.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/counterparties")]
[Authorize]
public sealed class CounterpartiesController(ICounterpartiesService counterparties) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? organizationIds,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken ct)
    {
        var parsed = OrgIdParser.Parse(organizationIds);
        if (page.HasValue)
        {
            var req = new PageRequest(page.Value, pageSize ?? 20, sortBy, sortDirection ?? "asc");
            return (await counterparties.GetPagedAsync(req, parsed, ct)).ToActionResult(this);
        }
        return (await counterparties.GetAllAsync(parsed, ct)).ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await counterparties.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CounterpartyDto dto, CancellationToken ct) =>
        (await counterparties.CreateAsync(dto, ct))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CounterpartyDto dto, CancellationToken ct) =>
        (await counterparties.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await counterparties.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

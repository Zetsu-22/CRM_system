using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Upds;
using CrmSystem.Application.Upds.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/upds")]
[Authorize]
public sealed class UpdsController(IUpdsService upds) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? organizationIds, CancellationToken ct) =>
        (await upds.GetAllAsync(OrgIdParser.Parse(organizationIds), ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await upds.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpdDto dto, CancellationToken ct) =>
        (await upds.CreateAsync(dto, ct))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdDto dto, CancellationToken ct) =>
        (await upds.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await upds.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

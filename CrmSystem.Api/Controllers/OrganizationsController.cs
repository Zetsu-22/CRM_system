using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Organizations;
using CrmSystem.Application.Organizations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/organizations")]
[Authorize]
public sealed class OrganizationsController(IOrganizationsService organizations) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        (await organizations.GetAllAsync(ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await organizations.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrganizationDto dto, CancellationToken ct) =>
        (await organizations.CreateAsync(dto, ct))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OrganizationDto dto, CancellationToken ct) =>
        (await organizations.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await organizations.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.ToActionResult(this);
    }
}

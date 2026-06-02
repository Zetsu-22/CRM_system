using CrmSystem.Api.Infrastructure;
using CrmSystem.Application.Companies;
using CrmSystem.Application.Companies.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
[Authorize]
public sealed class CompaniesController(ICompaniesService companies) : ControllerBase
{
    // GET /companies — возвращает компанию текущего пользователя
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        (await companies.GetAllAsync(ct)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await companies.GetByIdAsync(id, ct)).ToActionResult(this);

    // GET /companies/mine — удобный алиас
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        (await companies.GetMineAsync(ct)).ToActionResult(this);

    // PUT /companies/{id} — только владелец
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompanyDto dto, CancellationToken ct) =>
        (await companies.UpdateAsync(id, dto, ct)).ToActionResult(this);
}

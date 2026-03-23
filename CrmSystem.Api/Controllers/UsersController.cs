using CrmSystem.Api.Services;
using CrmSystem.Persistence.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Api.Controllers;

/// <summary>
/// Управление пользователями компании.
/// Только IsCompanyOwner или IsBasicProfile могут управлять пользователями.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(AuthDbContext auth, CurrentUserService currentUser) : ControllerBase
{
    // ── GET /api/v1/users ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!currentUser.CanManageUsers)
            return Forbid();

        var companyId = currentUser.CompanyId;
        var users = await auth.Users
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.FullName ?? u.Email)
            .Select(u => MapToDto(u))
            .ToListAsync(ct);

        return Ok(users);
    }

    // ── GET /api/v1/users/{id} ────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers)
            return Forbid();

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == currentUser.CompanyId, ct);
        return user is null ? NotFound() : Ok(MapToDto(user));
    }

    // ── POST /api/v1/users ────────────────────────────────────────────────────
    // Создаёт нового сотрудника в рамках текущей компании.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "Email и пароль обязательны" });

        if (dto.Password.Length < 6)
            return BadRequest(new { error = "Пароль должен быть не короче 6 символов" });

        if (await auth.Users.AnyAsync(u => u.Email == dto.Email.ToLower().Trim(), ct))
            return Conflict(new { error = "Пользователь с таким email уже существует" });

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = PasswordHelper.Hash(dto.Password),
            FullName = dto.FullName?.Trim(),
            Phone = dto.Phone?.Trim(),
            CompanyId = currentUser.CompanyId,
            IsCompanyOwner = false,
            IsBasicProfile = dto.IsBasicProfile,
            IsAccessOrdersEdit = dto.IsAccessOrdersEdit,
            IsOrderPicker = dto.IsOrderPicker,
            IsExpeditor = dto.IsExpeditor,
            IsAccessContractors = dto.IsAccessContractors,
            IsAccessContractorsEdit = dto.IsAccessContractorsEdit,
            IsAccessContractorDiscountEdit = dto.IsAccessContractorDiscountEdit,
            IsAccessNomenclature = dto.IsAccessNomenclature,
            IsAccessNomenclatureEdit = dto.IsAccessNomenclatureEdit,
            IsAccessFinancialJournal = dto.IsAccessFinancialJournal,
            IsAccessWarehouses = dto.IsAccessWarehouses,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        auth.Users.Add(user);
        await auth.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapToDto(user));
    }

    // ── PUT /api/v1/users/{id}/permissions ────────────────────────────────────
    // Обновляет права доступа сотрудника. Нельзя изменить права владельца компании.
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsDto dto, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers)
            return Forbid();

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == currentUser.CompanyId, ct);
        if (user is null) return NotFound();

        if (user.IsCompanyOwner)
            return Conflict(new { error = "Нельзя изменить права владельца компании" });

        user.IsBasicProfile = dto.IsBasicProfile;
        user.IsAccessOrdersEdit = dto.IsAccessOrdersEdit;
        user.IsOrderPicker = dto.IsOrderPicker;
        user.IsExpeditor = dto.IsExpeditor;
        user.IsAccessContractors = dto.IsAccessContractors;
        user.IsAccessContractorsEdit = dto.IsAccessContractorsEdit;
        user.IsAccessContractorDiscountEdit = dto.IsAccessContractorDiscountEdit;
        user.IsAccessNomenclature = dto.IsAccessNomenclature;
        user.IsAccessNomenclatureEdit = dto.IsAccessNomenclatureEdit;
        user.IsAccessFinancialJournal = dto.IsAccessFinancialJournal;
        user.IsAccessWarehouses = dto.IsAccessWarehouses;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await auth.SaveChangesAsync(ct);
        return Ok(MapToDto(user));
    }

    // ── PUT /api/v1/users/{id}/profile ────────────────────────────────────────
    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateUserProfileDto dto, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers)
            return Forbid();

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == currentUser.CompanyId, ct);
        if (user is null) return NotFound();

        user.FullName = dto.FullName?.Trim();
        user.Phone = dto.Phone?.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);
        return Ok(MapToDto(user));
    }

    // ── PUT /api/v1/users/{id}/activate ──────────────────────────────────────
    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers) return Forbid();
        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == currentUser.CompanyId, ct);
        if (user is null) return NotFound();
        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);
        return Ok(MapToDto(user));
    }

    // ── PUT /api/v1/users/{id}/deactivate ────────────────────────────────────
    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers) return Forbid();
        if (id == currentUser.UserId)
            return BadRequest(new { error = "Нельзя заблокировать самого себя" });

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == currentUser.CompanyId, ct);
        if (user is null) return NotFound();
        if (user.IsCompanyOwner)
            return Conflict(new { error = "Нельзя заблокировать владельца компании" });

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);
        return Ok(MapToDto(user));
    }

    // ── PUT /api/v1/users/{id}/reset-password ────────────────────────────────
    [HttpPut("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        if (!currentUser.CanManageUsers) return Forbid();
        if (dto.NewPassword.Length < 6)
            return BadRequest(new { error = "Пароль должен быть не короче 6 символов" });

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == currentUser.CompanyId, ct);
        if (user is null) return NotFound();

        user.PasswordHash = PasswordHelper.Hash(dto.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static UserDto MapToDto(AuthUser u) => new(
        u.Id, u.Email, u.FullName, u.Phone, u.IsActive,
        u.CompanyId, u.IsCompanyOwner, u.IsBasicProfile,
        u.IsAccessOrdersEdit, u.IsOrderPicker, u.IsExpeditor,
        u.IsAccessContractors, u.IsAccessContractorsEdit, u.IsAccessContractorDiscountEdit,
        u.IsAccessNomenclature, u.IsAccessNomenclatureEdit,
        u.IsAccessFinancialJournal, u.IsAccessWarehouses);
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record UserDto(
    Guid Id, string Email, string? FullName, string? Phone, bool IsActive,
    Guid? CompanyId, bool IsCompanyOwner, bool IsBasicProfile,
    bool IsAccessOrdersEdit, bool IsOrderPicker, bool IsExpeditor,
    bool IsAccessContractors, bool IsAccessContractorsEdit, bool IsAccessContractorDiscountEdit,
    bool IsAccessNomenclature, bool IsAccessNomenclatureEdit,
    bool IsAccessFinancialJournal, bool IsAccessWarehouses);

public sealed record CreateUserDto(
    string Email, string Password,
    string? FullName, string? Phone,
    bool IsBasicProfile = false,
    bool IsAccessOrdersEdit = false,
    bool IsOrderPicker = false,
    bool IsExpeditor = false,
    bool IsAccessContractors = false,
    bool IsAccessContractorsEdit = false,
    bool IsAccessContractorDiscountEdit = false,
    bool IsAccessNomenclature = false,
    bool IsAccessNomenclatureEdit = false,
    bool IsAccessFinancialJournal = false,
    bool IsAccessWarehouses = false);

public sealed record UpdatePermissionsDto(
    bool IsBasicProfile,
    bool IsAccessOrdersEdit,
    bool IsOrderPicker,
    bool IsExpeditor,
    bool IsAccessContractors,
    bool IsAccessContractorsEdit,
    bool IsAccessContractorDiscountEdit,
    bool IsAccessNomenclature,
    bool IsAccessNomenclatureEdit,
    bool IsAccessFinancialJournal,
    bool IsAccessWarehouses);

public sealed record UpdateUserProfileDto(string? FullName, string? Phone);

public sealed record ResetPasswordDto(string NewPassword);

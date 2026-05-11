using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CrmSystem.Api.Services;
using CrmSystem.Domain;
using CrmSystem.Persistence;
using CrmSystem.Persistence.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrmSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AuthDbContext auth,
    CrmDbContext crm,
    IConfiguration config,
    CurrentUserService currentUser) : ControllerBase
{
    // ── POST /api/v1/auth/register ────────────────────────────────────────────
    // Регистрирует пользователя. Компания создаётся отдельно через /setup-company.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "Email и пароль обязательны" });

        var email = dto.Email.Trim().ToLowerInvariant();

        if (dto.Password.Length < 6)
            return BadRequest(new { error = "Пароль должен быть не короче 6 символов" });

        if (await auth.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { error = "Пользователь с таким email уже зарегистрирован" });

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = PasswordHelper.Hash(dto.Password),
            FullName = dto.FullName?.Trim(),
            Phone = dto.Phone?.Trim(),
            IsBasicProfile = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        auth.Users.Add(user);
        try
        {
            await auth.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Most common case: unique index on users.email.
            return Conflict(new { error = "Пользователь с таким email уже зарегистрирован" });
        }

        var profiles = await BuildAvailableProfiles(user, ct);
        var pair = await IssueTokenPairAsync(user, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(user, null, profiles) });
    }

    // ── POST /api/v1/auth/setup-company ──────────────────────────────────────
    // Создаёт компанию и делает текущего пользователя её владельцем.
    [HttpPost("setup-company")]
    [Authorize]
    public async Task<IActionResult> SetupCompany([FromBody] SetupCompanyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return BadRequest(new { error = "Введите название компании" });

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null) return NotFound();

        if (user.CompanyId.HasValue)
            return Conflict(new { error = "Компания уже создана" });

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = dto.CompanyName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        crm.Companies.Add(company);
        await crm.SaveChangesAsync(ct);

        user.CompanyId = company.Id;
        user.IsCompanyOwner = true;
        user.IsBasicProfile = true;
        user.IsAccessOrdersEdit = true;
        user.IsOrderPicker = true;
        user.IsExpeditor = true;
        user.IsDriver = true;
        user.IsAccessContractors = true;
        user.IsAccessContractorsEdit = true;
        user.IsAccessContractorDiscountEdit = true;
        user.IsAccessNomenclature = true;
        user.IsAccessNomenclatureEdit = true;
        user.IsAccessFinancialJournal = true;
        user.IsAccessFinancialJournalEdit = true;
        user.IsAccessWarehouses = true;
        user.IsAccessWarehousesEdit = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);

        var setupProfiles = await BuildAvailableProfiles(user, ct);
        var pair = await IssueTokenPairAsync(user, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(user, company.Name, setupProfiles) });
    }

    // ── POST /api/v1/auth/login ───────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "Заполните все поля" });

        var email = dto.Email.ToLowerInvariant().Trim();

        var user = await auth.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !PasswordHelper.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { error = "Неверный email или пароль" });

        if (!user.IsActive)
            return Unauthorized(new { error = "Аккаунт заблокирован. Обратитесь к администратору" });

        string? companyName = null;
        if (user.CompanyId.HasValue)
        {
            companyName = await crm.Companies
                .Where(c => c.Id == user.CompanyId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        var loginProfiles = await BuildAvailableProfiles(user, ct);
        var pair = await IssueTokenPairAsync(user, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(user, companyName, loginProfiles) });
    }

    // ── POST /api/v1/auth/refresh ──────────────────────────────────────────────────────────────
    // Принимает refresh-токен, ротирует его и выдаёт новую пару access+refresh.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest(new { error = "Не передан refresh-токен" });

        var hash = HashToken(dto.RefreshToken);
        var existing = await auth.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
            return Unauthorized(new { error = "refresh_token_invalid" });

        if (existing.User is null || !existing.User.IsActive)
            return Unauthorized(new { error = "Аккаунт недоступен" });

        var pair = await IssueTokenPairAsync(existing.User, ct);

        // Ротируем старый
        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.ReplacedByTokenHash = HashToken(pair.RefreshToken);
        await auth.SaveChangesAsync(ct);

        string? companyName = null;
        if (existing.User.CompanyId.HasValue)
        {
            companyName = await crm.Companies
                .Where(c => c.Id == existing.User.CompanyId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        var profiles = await BuildAvailableProfiles(existing.User, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(existing.User, companyName, profiles) });
    }

    // ── POST /api/v1/auth/logout ─────────────────────────────────────────────────────────────────
    // Ревокация refresh-токена. Access-токен живёт до своего естественного истечения.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return NoContent();

        var hash = HashToken(dto.RefreshToken);
        var existing = await auth.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTimeOffset.UtcNow;
            await auth.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    // ── GET /api/v1/auth/me ────────────────────────────────────────────────────
    // Returns fresh token + profile so stale tokens in localStorage are self-healed.
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null) return NotFound();

        string? companyName = null;
        if (user.CompanyId.HasValue)
        {
            companyName = await crm.Companies
                .Where(c => c.Id == user.CompanyId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);
        }

        var profiles = await BuildAvailableProfiles(user, ct);
        var pair = await IssueTokenPairAsync(user, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(user, companyName, profiles) });
    }

    // /me намеренно не выдаёт refresh — FE вызывает /refresh явно.

    // ── GET /api/v1/auth/profiles ────────────────────────────────────────────
    // Список доступных профилей (базовый + компании) — как в fimbiz.
    [HttpGet("profiles")]
    [Authorize]
    public async Task<IActionResult> GetProfiles(CancellationToken ct)
    {
        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null) return NotFound();
        return Ok(await BuildAvailableProfiles(user, ct));
    }

    // ── POST /api/v1/auth/switch-company ─────────────────────────────────────
    // Переключение на другую компанию → новый JWT с companyId.
    // companyId=null → базовый профиль (без компании).
    [HttpPost("switch-company")]
    [Authorize]
    public async Task<IActionResult> SwitchCompany([FromBody] SwitchCompanyDto dto, CancellationToken ct)
    {
        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null) return NotFound();

        string? companyName = null;

        if (dto.CompanyId.HasValue)
        {
            // Проверяем, что пользователь имеет доступ к этой компании
            // (он её владелец, или привязан к ней)
            var company = await crm.Companies.FirstOrDefaultAsync(c => c.Id == dto.CompanyId.Value, ct);
            if (company is null) return NotFound(new { error = "Компания не найдена" });

            // Проверяем: пользователь либо владелец этой компании, либо привязан
            var hasAccess = user.CompanyId == dto.CompanyId.Value;
            // Позже можно расширить: проверять таблицу user_companies для мульти-компаний
            if (!hasAccess)
                return Forbid();

            companyName = company.Name;
        }

        // Генерируем новый JWT с нужным companyId (или null для базового профиля)
        var switchedUser = new AuthUser
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            CompanyId = dto.CompanyId,
            IsCompanyOwner = dto.CompanyId.HasValue && user.CompanyId == dto.CompanyId && user.IsCompanyOwner,
            IsBasicProfile = user.IsBasicProfile,
            IsAccessOrdersEdit = user.IsAccessOrdersEdit,
            IsOrderPicker = user.IsOrderPicker,
            IsExpeditor = user.IsExpeditor,
            IsDriver = user.IsDriver,
            IsAccessContractors = user.IsAccessContractors,
            IsAccessContractorsEdit = user.IsAccessContractorsEdit,
            IsAccessContractorDiscountEdit = user.IsAccessContractorDiscountEdit,
            IsAccessNomenclature = user.IsAccessNomenclature,
            IsAccessNomenclatureEdit = user.IsAccessNomenclatureEdit,
            IsAccessFinancialJournal = user.IsAccessFinancialJournal,
            IsAccessFinancialJournalEdit = user.IsAccessFinancialJournalEdit,
            IsAccessWarehouses = user.IsAccessWarehouses,
            IsAccessWarehousesEdit = user.IsAccessWarehousesEdit,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };

        var profiles = await BuildAvailableProfiles(user, ct);
        var pair = await IssueTokenPairAsync(switchedUser, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(switchedUser, companyName, profiles) });
    }

    // ── PUT /api/v1/auth/me ────────────────────────────────────────────────────
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null) return NotFound();

        user.FullName = dto.FullName?.Trim();
        user.Phone = dto.Phone?.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);

        var pair = await IssueTokenPairAsync(user, ct);
        return Ok(new { token = pair.AccessToken, refreshToken = pair.RefreshToken, profile = BuildProfile(user, null) });
    }


    // ── PUT /api/v1/auth/me/password ──────────────────────────────────────────
    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        if (dto.NewPassword.Length < 6)
            return BadRequest(new { error = "Новый пароль должен быть не короче 6 символов" });

        var user = await auth.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null) return NotFound();

        if (!PasswordHelper.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { error = "Текущий пароль неверен" });

        user.PasswordHash = PasswordHelper.Hash(dto.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.SaveChangesAsync(ct);

        return NoContent();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(string AccessToken, string RefreshToken, DateTimeOffset RefreshExpiresAt)> IssueTokenPairAsync(AuthUser user, CancellationToken ct)
    {
        var access = GenerateAccessToken(user);
        var refresh = GenerateRefreshTokenString();
        var refreshDays = config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(refreshDays);

        auth.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refresh),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
        });
        await auth.SaveChangesAsync(ct);

        return (access, refresh, expiresAt);
    }

    private static string GenerateRefreshTokenString()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GenerateAccessToken(AuthUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName ?? user.Email),
            // Уровни доступа
            new("IsCompanyOwner",  user.IsCompanyOwner.ToString().ToLower()),
            new("IsBasicProfile",  user.IsBasicProfile.ToString().ToLower()),
            // Права к модулям
            new("IsAccessOrdersEdit",              user.IsAccessOrdersEdit.ToString().ToLower()),
            new("IsOrderPicker",                   user.IsOrderPicker.ToString().ToLower()),
            new("IsExpeditor",                     user.IsExpeditor.ToString().ToLower()),
            new("IsDriver",                         user.IsDriver.ToString().ToLower()),
            new("IsAccessContractors",             user.IsAccessContractors.ToString().ToLower()),
            new("IsAccessContractorsEdit",         user.IsAccessContractorsEdit.ToString().ToLower()),
            new("IsAccessContractorDiscountEdit",  user.IsAccessContractorDiscountEdit.ToString().ToLower()),
            new("IsAccessNomenclature",            user.IsAccessNomenclature.ToString().ToLower()),
            new("IsAccessNomenclatureEdit",        user.IsAccessNomenclatureEdit.ToString().ToLower()),
            new("IsAccessFinancialJournal",        user.IsAccessFinancialJournal.ToString().ToLower()),
            new("IsAccessFinancialJournalEdit",    user.IsAccessFinancialJournalEdit.ToString().ToLower()),
            new("IsAccessWarehouses",              user.IsAccessWarehouses.ToString().ToLower()),
            new("IsAccessWarehousesEdit",          user.IsAccessWarehousesEdit.ToString().ToLower()),
        };

        if (user.CompanyId.HasValue)
            claims.Add(new("companyId", user.CompanyId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static object BuildProfile(AuthUser user, string? companyName, object[]? availableProfiles = null) => new
    {
        id = user.Id,
        email = user.Email,
        fullName = user.FullName,
        phone = user.Phone,
        companyId = user.CompanyId,
        companyName,
        isCompanyOwner = user.IsCompanyOwner,
        isBasicProfile = user.IsBasicProfile,
        isAccessOrdersEdit = user.IsAccessOrdersEdit,
        isOrderPicker = user.IsOrderPicker,
        isExpeditor = user.IsExpeditor,
        isDriver = user.IsDriver,
        isAccessContractors = user.IsAccessContractors,
        isAccessContractorsEdit = user.IsAccessContractorsEdit,
        isAccessContractorDiscountEdit = user.IsAccessContractorDiscountEdit,
        isAccessNomenclature = user.IsAccessNomenclature,
        isAccessNomenclatureEdit = user.IsAccessNomenclatureEdit,
        isAccessFinancialJournal = user.IsAccessFinancialJournal,
        isAccessFinancialJournalEdit = user.IsAccessFinancialJournalEdit,
        isAccessWarehouses = user.IsAccessWarehouses,
        isAccessWarehousesEdit = user.IsAccessWarehousesEdit,
        availableProfiles = availableProfiles ?? Array.Empty<object>(),
    };

    /// <summary>
    /// Строит список доступных профилей (как в fimbiz):
    /// 1) Базовый профиль (companyId=null, name="Личный профиль")
    /// 2) Компании пользователя
    /// </summary>
    private async Task<object[]> BuildAvailableProfiles(AuthUser user, CancellationToken ct)
    {
        var profiles = new List<object>();

        // 1. Базовый (личный) профиль — всегда есть
        profiles.Add(new
        {
            companyId = (Guid?)null,
            name = user.FullName ?? "Личный профиль",
            role = "Личный",
            isBasicProfile = true,
            isCompanyOwner = false,
        });

        // 2. Компании пользователя
        if (user.CompanyId.HasValue)
        {
            var companyName = await crm.Companies
                .Where(c => c.Id == user.CompanyId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct);

            profiles.Add(new
            {
                companyId = (Guid?)user.CompanyId.Value,
                name = companyName ?? "Компания",
                role = user.IsCompanyOwner ? "Владелец" : "Сотрудник",
                isBasicProfile = false,
                isCompanyOwner = user.IsCompanyOwner,
            });
        }

        // Позже: добавить компании из таблицы user_companies для мульти-компаний

        return profiles.ToArray();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record RegisterDto(
    string Email,
    string Password,
    string? FullName,
    string? Phone);

public sealed record SetupCompanyDto(string CompanyName);

public sealed record SwitchCompanyDto(Guid? CompanyId);

public sealed record LoginDto(string Email, string Password);

public sealed record RefreshTokenDto(string RefreshToken);

public sealed record UpdateProfileDto(string? FullName, string? Phone);

public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword);

// ── Password Helper (PBKDF2 / RFC 2898) ──────────────────────────────────────
internal static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}

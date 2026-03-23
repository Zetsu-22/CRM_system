using System.Security.Claims;

namespace CrmSystem.Api.Services;

/// <summary>
/// Читает данные текущего аутентифицированного пользователя из JWT-токена.
/// Используется во всех контроллерах для проверки прав и tenant-фильтрации.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => Guid.Parse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("UserId claim not found"));

    public Guid? CompanyId
    {
        get
        {
            var v = Principal?.FindFirstValue("companyId")
                ?? Principal?.FindFirstValue("CompanyId")
                ?? Principal?.FindFirstValue("companyID")
                ?? Principal?.FindFirstValue("CompanyID");
            if (string.IsNullOrWhiteSpace(v)) return null;
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string FullName => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    // ── Уровни доступа ────────────────────────────────────────────────────
    public bool IsCompanyOwner => GetBool("IsCompanyOwner");
    public bool IsBasicProfile => GetBool("IsBasicProfile");

    /// <summary>Full access: owner OR basic (admin) profile — mirrors fimbiz logic.</summary>
    private bool HasFullAccess => IsCompanyOwner || IsBasicProfile;

    // ── Права к модулям ───────────────────────────────────────────────────
    public bool IsAccessOrdersEdit       => HasFullAccess || GetBool("IsAccessOrdersEdit");
    public bool IsOrderPicker            => HasFullAccess || GetBool("IsOrderPicker");
    public bool IsExpeditor              => HasFullAccess || GetBool("IsExpeditor");
    public bool IsAccessContractors      => HasFullAccess || GetBool("IsAccessContractors");
    public bool IsAccessContractorsEdit  => HasFullAccess || GetBool("IsAccessContractorsEdit");
    public bool IsAccessContractorDiscountEdit => HasFullAccess || GetBool("IsAccessContractorDiscountEdit");
    public bool IsAccessNomenclature     => HasFullAccess || GetBool("IsAccessNomenclature");
    public bool IsAccessNomenclatureEdit => HasFullAccess || GetBool("IsAccessNomenclatureEdit");
    public bool IsAccessFinancialJournal => HasFullAccess || GetBool("IsAccessFinancialJournal");
    public bool IsAccessWarehouses       => HasFullAccess || GetBool("IsAccessWarehouses");

    /// <summary>
    /// Может управлять пользователями компании (добавлять, редактировать права).
    /// </summary>
    public bool CanManageUsers => IsCompanyOwner || IsBasicProfile;

    /// <summary>
    /// Проверяет, что текущий пользователь принадлежит указанной компании.
    /// IsCompanyOwner не обходит эту проверку — он всё равно изолирован своей компанией.
    /// </summary>
    public bool BelongsToCompany(Guid companyId) => CompanyId == companyId;

    private bool GetBool(string claimType) =>
        Principal?.FindFirstValue(claimType) == "true";
}

namespace CrmSystem.Domain;

/// <summary>
/// Сущность принадлежит конкретной компании (tenant).
/// <see cref="TenantSaveChangesInterceptor"/> следит, чтобы при сохранении
/// CompanyId совпадал с CompanyId текущего пользователя.
/// </summary>
public interface ITenantOwned
{
    Guid CompanyId { get; }
}

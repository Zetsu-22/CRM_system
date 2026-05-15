using CrmSystem.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CrmSystem.Persistence.Interceptors;

/// <summary>
/// Валидирует, что все Added/Modified ITenantOwned-сущности имеют CompanyId,
/// совпадающий с CompanyId текущего пользователя. Дублирует фильтры
/// в контроллерах как второй слой защиты.
/// </summary>
public sealed class TenantSaveChangesInterceptor(ICurrentUserAccessor currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Validate(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Validate(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Validate(DbContext? ctx)
    {
        if (ctx is null) return;

        // Если пользователь не аутентифицирован или не привязан к компании
        // (миграции, seed, сама таблица users), пропускаем — пусть отрабатывают
        // обычные ограничения БД. Это сознательное решение: interceptor защищает
        // только операции от лица конкретного tenant'а.
        var currentCompanyId = currentUser.CompanyId;
        if (currentCompanyId is null) return;

        foreach (var entry in ctx.ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            if (entry.Entity.CompanyId != currentCompanyId.Value)
            {
                throw new InvalidOperationException(
                    $"Tenant violation: entity '{entry.Metadata.ClrType.Name}' has CompanyId={entry.Entity.CompanyId} " +
                    $"but current user's CompanyId={currentCompanyId.Value}.");
            }
        }
    }
}

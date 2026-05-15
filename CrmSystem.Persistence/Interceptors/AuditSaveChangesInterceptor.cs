using CrmSystem.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CrmSystem.Persistence.Interceptors;

/// <summary>
/// На SaveChanges проставляет CreatedAt/By и UpdatedAt/By для всех AuditableEntity.
/// Перезаписывает значения, пришедшие с клиента (client cannot spoof timestamps).
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUserAccessor currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? ctx)
    {
        if (ctx is null) return;

        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in ctx.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    if (userId.HasValue)
                    {
                        entry.Entity.CreatedBy = userId.Value;
                        entry.Entity.UpdatedBy = userId.Value;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    if (userId.HasValue)
                        entry.Entity.UpdatedBy = userId.Value;

                    // Never let client-supplied CreatedAt/CreatedBy overwrite initial values.
                    entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(AuditableEntity.CreatedBy)).IsModified = false;
                    break;
            }
        }
    }
}

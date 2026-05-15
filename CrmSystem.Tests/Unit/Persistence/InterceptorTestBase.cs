using CrmSystem.Persistence;
using CrmSystem.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Tests.Unit.Persistence;

internal sealed class StubCurrentUser : ICurrentUserAccessor
{
    public bool IsAuthenticated { get; set; } = true;
    public Guid? UserId { get; set; }
    public Guid? CompanyId { get; set; }
}

internal static class TestContextFactory
{
    public static CrmDbContext Create(ICurrentUserAccessor currentUser, params Type[] _)
    {
        var dbName = Guid.NewGuid().ToString();
        var opts = new DbContextOptionsBuilder<CrmDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(currentUser),
                new AuditSaveChangesInterceptor(currentUser))
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new CrmDbContext(opts);
    }
}

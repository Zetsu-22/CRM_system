using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrmSystem.Persistence;

/// <summary>Design-time factory — позволяет запускать ef migrations без запущенного API.</summary>
public sealed class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    public CrmDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=crm_main;Username=postgres;Password=123")
            .Options;
        return new CrmDbContext(opts);
    }
}

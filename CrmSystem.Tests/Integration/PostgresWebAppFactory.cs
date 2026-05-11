using CrmSystem.Persistence;
using CrmSystem.Persistence.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace CrmSystem.Tests.Integration;

/// <summary>
/// Поднимает API в памяти + временный Postgres через Testcontainers.
/// Использовать только в тестах с [Trait("Category","Integration")].
/// </summary>
public sealed class PostgresWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("crm_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            var cs = _db.GetConnectionString();
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CrmDb"] = cs,
                ["ConnectionStrings:AuthDb"] = cs,
                ["Jwt:Key"] = "TEST_SUPER_SECRET_KEY_FOR_INTEGRATION_TESTS_!!",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Применяем миграции на свежий контейнер до старта приложения.
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
            scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.Migrate();
        });
    }
}

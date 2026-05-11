using System.Net.Http.Json;
using FluentAssertions;

namespace CrmSystem.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AuthEndpointTests : IClassFixture<PostgresWebAppFactory>
{
    private readonly PostgresWebAppFactory _factory;

    public AuthEndpointTests(PostgresWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Then_Login_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var email = $"u{Guid.NewGuid():N}@test.local";
        var password = "P@ssw0rd!";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Integration Tester",
            phone = (string?)null,
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        var payload = await login.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        payload.Should().NotBeNull();
        payload!.Should().ContainKey("token");
        payload["token"].ToString().Should().NotBeNullOrWhiteSpace();
    }
}

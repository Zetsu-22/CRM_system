using FluentAssertions;

namespace CrmSystem.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HealthEndpointTests : IClassFixture<PostgresWebAppFactory>
{
    private readonly PostgresWebAppFactory _factory;

    public HealthEndpointTests(PostgresWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task LegacyHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/health");
        resp.EnsureSuccessStatusCode();
    }
}

using CrmSystem.Domain;
using FluentAssertions;

namespace CrmSystem.Tests.Unit.Persistence;

public sealed class TenantInterceptorTests
{
    private static readonly Guid MyCompany = Guid.NewGuid();
    private static readonly Guid OtherCompany = Guid.NewGuid();

    [Fact]
    public async Task Save_Entity_With_Own_Company_Succeeds()
    {
        var cu = new StubCurrentUser { CompanyId = MyCompany, UserId = Guid.NewGuid() };
        await using var ctx = TestContextFactory.Create(cu);

        ctx.Counterparties.Add(new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = MyCompany,
            Name = "ACME",
            Type = CounterpartyType.Customer,
            Status = CounterpartyStatus.Active,
        });

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_Entity_With_Foreign_Company_Throws()
    {
        var cu = new StubCurrentUser { CompanyId = MyCompany, UserId = Guid.NewGuid() };
        await using var ctx = TestContextFactory.Create(cu);

        ctx.Counterparties.Add(new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = OtherCompany, // подделка
            Name = "Evil Corp",
            Type = CounterpartyType.Customer,
            Status = CounterpartyStatus.Active,
        });

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant violation*");
    }

    [Fact]
    public async Task Save_When_User_Has_No_Company_Is_Allowed()
    {
        // Сценарий: миграция/seed/фоновая задача — interceptor пропускает.
        var cu = new StubCurrentUser { CompanyId = null, UserId = null };
        await using var ctx = TestContextFactory.Create(cu);

        ctx.Counterparties.Add(new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = OtherCompany,
            Name = "Seeded",
            Type = CounterpartyType.Customer,
            Status = CounterpartyStatus.Active,
        });

        var act = async () => await ctx.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }
}

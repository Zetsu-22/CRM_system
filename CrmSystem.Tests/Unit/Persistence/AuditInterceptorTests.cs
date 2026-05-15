using CrmSystem.Domain;
using FluentAssertions;

namespace CrmSystem.Tests.Unit.Persistence;

public sealed class AuditInterceptorTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public async Task Added_Entity_Gets_CreatedAt_UpdatedAt_And_User()
    {
        var userId = Guid.NewGuid();
        var cu = new StubCurrentUser { UserId = userId, CompanyId = CompanyId };
        await using var ctx = TestContextFactory.Create(cu);

        var entity = new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            Name = "ACME",
            Type = CounterpartyType.Customer,
            Status = CounterpartyStatus.Active,
            // Намеренно не ставим CreatedAt/UpdatedAt — interceptor заполнит.
        };
        ctx.Counterparties.Add(entity);

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await ctx.SaveChangesAsync();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        entity.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        entity.UpdatedAt.Should().Be(entity.CreatedAt);
        entity.CreatedBy.Should().Be(userId);
        entity.UpdatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task Modified_Entity_Updates_UpdatedAt_But_Not_CreatedAt()
    {
        var originalUser = Guid.NewGuid();
        var cu = new StubCurrentUser { UserId = originalUser, CompanyId = CompanyId };
        await using var ctx = TestContextFactory.Create(cu);

        var entity = new Counterparty
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            Name = "ACME",
            Type = CounterpartyType.Customer,
            Status = CounterpartyStatus.Active,
        };
        ctx.Counterparties.Add(entity);
        await ctx.SaveChangesAsync();

        var originalCreatedAt = entity.CreatedAt;

        // Имитируем смену пользователя (другой сотрудник редактирует)
        var editorUser = Guid.NewGuid();
        cu.UserId = editorUser;

        await Task.Delay(10);
        entity.Name = "ACME Inc.";
        entity.CreatedAt = DateTimeOffset.MinValue;   // попытка подмены — должна быть отброшена
        entity.CreatedBy = Guid.NewGuid();            // попытка подмены — должна быть отброшена
        await ctx.SaveChangesAsync();

        entity.CreatedAt.Should().Be(originalCreatedAt, "audit interceptor должен запрещать изменение CreatedAt");
        entity.CreatedBy.Should().Be(originalUser);
        entity.UpdatedAt.Should().BeAfter(originalCreatedAt);
        entity.UpdatedBy.Should().Be(editorUser);
    }
}

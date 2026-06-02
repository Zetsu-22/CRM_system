namespace CrmSystem.Api.Infrastructure;

/// <summary>
/// Разбирает строку вида "guid1,guid2,..." в коллекцию GUID-ов.
/// Используется тонкими контроллерами для передачи ?organizationIds= в сервисы.
/// </summary>
internal static class OrgIdParser
{
    public static IReadOnlyCollection<Guid>? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}

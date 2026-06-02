namespace CrmSystem.Application.Common;

/// <summary>Обёртка страничного ответа. Используется когда клиент передаёт ?page= параметр.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

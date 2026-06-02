using CrmSystem.Domain;

namespace CrmSystem.Application.Orders.Dtos;

/// <summary>
/// Параметры фильтрации и поиска заказов (шаг 6.11).
/// Все поля опциональны — если не задано, фильтр не применяется.
/// </summary>
public sealed record OrderFilterRequest(
    /// <summary>Поиск по номеру заказа, имени контрагента или комментарию (case-insensitive).</summary>
    string? SearchQuery,

    /// <summary>Фильтр по одному или нескольким статусам.</summary>
    IReadOnlyList<OrderStatus>? Statuses,

    /// <summary>Фильтр по конкретному контрагенту.</summary>
    Guid? CounterpartyId,

    /// <summary>Начало диапазона дат заказа (включительно).</summary>
    DateOnly? DateFrom,

    /// <summary>Конец диапазона дат заказа (включительно).</summary>
    DateOnly? DateTo,

    /// <summary>Минимальная итоговая сумма (TotalGross).</summary>
    decimal? TotalFrom,

    /// <summary>Максимальная итоговая сумма (TotalGross).</summary>
    decimal? TotalTo
)
{
    public static readonly OrderFilterRequest Empty = new(null, null, null, null, null, null, null);

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(SearchQuery) &&
        (Statuses is null || Statuses.Count == 0) &&
        CounterpartyId is null &&
        DateFrom is null && DateTo is null &&
        TotalFrom is null && TotalTo is null;
}

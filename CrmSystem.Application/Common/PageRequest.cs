namespace CrmSystem.Application.Common;

/// <summary>Параметры страничной навигации и сортировки.</summary>
public sealed record PageRequest(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = "asc")
{
    public int Skip => (Math.Max(1, Page) - 1) * Math.Clamp(PageSize, 1, 200);
    public int Take => Math.Clamp(PageSize, 1, 200);
}

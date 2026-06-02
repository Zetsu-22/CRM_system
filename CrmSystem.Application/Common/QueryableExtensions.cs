using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Application.Common;

public static class QueryableExtensions
{
    /// <summary>
    /// Выполняет COUNT + страничный SELECT и оборачивает результат в <see cref="PagedResponse{T}"/>.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(ct);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.Take);
        return new PagedResponse<T>(items, Math.Max(1, request.Page), request.Take, total, totalPages);
    }
}

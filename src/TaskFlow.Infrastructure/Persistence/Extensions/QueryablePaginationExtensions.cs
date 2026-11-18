using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Infrastructure.Persistence.Extensions;

public static class QueryablePaginationExtensions
{
    // The query must already be ordered so pages are stable.
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PaginationRm pagination, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, pagination.Page, pagination.PageSize, totalCount);
    }
}

using eNote.Application.Common.Search;

namespace eNote.Application.Common.Paging;

public static class PagingExtensions
{
    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TModel>(this IQueryable<TEntity> query, TSearch search, Func<TEntity, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default) where TSearch : BaseSearchObject
    {
        var (page, pageSize, total, entities) = await FetchPageAsync(query, search, orderBy, ct);

        return new PagedResult<TModel>
        {
            Items = [.. entities.Select(map)],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TModel>(this IQueryable<TEntity> query, TSearch search, Func<TEntity, Task<TModel>> mapAsync, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default) where TSearch : BaseSearchObject
    {
        var (page, pageSize, total, entities) = await FetchPageAsync(query, search, orderBy, ct);

        var items = await Task.WhenAll(entities.Select(mapAsync));

        return new PagedResult<TModel>
        {
            Items = [.. items],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static async Task<(int Page, int PageSize, int? TotalCount, List<TEntity> Entities)> FetchPageAsync<TEntity, TSearch>(IQueryable<TEntity> query, TSearch search, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy, CancellationToken ct) where TSearch : BaseSearchObject
    {
        int? total = null;

        if (search.IncludeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (int page, int pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (page, pageSize, total, entities);
    }
}

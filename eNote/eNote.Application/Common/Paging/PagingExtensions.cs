using eNote.Application.Common.Search;

namespace eNote.Application.Common.Paging;

public static class PagingExtensions
{
    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TModel>(this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<TEntity, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
    {
        int? total = null;

        if (includeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (page, pageSize) = PagingLimits.Normalize(page, pageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TModel>
        {
            Items = [.. entities.Select(map)],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TModel>
        (this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<TEntity, Task<TModel>> mapAsync, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
    {
        int? total = null;

        if (includeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (page, pageSize) = PagingLimits.Normalize(page, pageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await Task.WhenAll(entities.Select(mapAsync));

        return new PagedResult<TModel>
        {
            Items = [.. items],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TCtx, TModel>
        (this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<IReadOnlyList<TEntity>, Task<TCtx>> loadContext, Func<TEntity, TCtx, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
    {
        int? total = null;

        if (includeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (page, pageSize) = PagingLimits.Normalize(page, pageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ctx = await loadContext(entities);

        return new PagedResult<TModel>
        {
            Items = [.. entities.Select(e => map(e, ctx))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TModel>
        (this IQueryable<TEntity> query, TSearch search, Func<TEntity, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
        where TSearch : BaseSearchObject => query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, map, orderBy, ct);

    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TModel>
        (this IQueryable<TEntity> query, TSearch search, Func<TEntity, Task<TModel>> mapAsync, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
        where TSearch : BaseSearchObject => query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, mapAsync, orderBy, ct);

    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TCtx, TModel>
        (this IQueryable<TEntity> query, TSearch search, Func<IReadOnlyList<TEntity>, Task<TCtx>> loadContext, Func<TEntity, TCtx, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
        where TSearch : BaseSearchObject => query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, loadContext, map, orderBy, ct);
}

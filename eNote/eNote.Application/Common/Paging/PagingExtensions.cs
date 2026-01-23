using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Paging
{
    public static class PagingExtensions
    {
        public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TModel>(this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<TEntity, TModel> map, CancellationToken ct = default)
        {
            int? total = null;

            if (includeTotalCount) 
                total = await query.CountAsync(ct);

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

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
    }
}

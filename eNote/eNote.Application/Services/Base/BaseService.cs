using eNote.Application.Interfaces.Base;
using eNote.Application.Interfaces.Ports;
using eNote.Application.SearchObjects;
using eNote.Domain.Entities.Base;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Services.Base
{
    public abstract class BaseService<TModel, TSearch, TDbEntity>(IAppDbContext context, IMapper mapper) 
        : IService<TModel, TSearch> where TModel : class where TDbEntity : class, IEntity where TSearch : BaseSearchObject
    {
        protected readonly IAppDbContext _context = context;
        protected readonly IMapper _mapper = mapper;

        public virtual async Task<TModel> GetByIdAsync(int id)
        {
            var query = _context.Set<TDbEntity>().AsNoTracking();   

            query = AddIncludes(query);

            query = AddIdFilter(query);

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("ID nije pronađen");

            return _mapper.Map<TModel>(entity);
        }

        public virtual async Task<PagedResult<TModel>> GetPagedAsync(TSearch search)
        {
            var query = _context.Set<TDbEntity>().AsNoTracking().AsQueryable();

            query = AddIncludes(query);

            query = AddFilter(search, query);

            int? totalCount = null;

            if (search.IncludeTotalCount)
            {
                totalCount = await query.CountAsync();
            }

            var page = search.Page < 1 ? 1 : search.Page;

            var pageSize = search.PageSize < 1 ? 20 : search.PageSize;

            query = query.Skip((page - 1) * pageSize).Take(pageSize);

            var entities = await query.ToListAsync();

            var models = _mapper.Map<List<TModel>>(entities);

            return new PagedResult<TModel>
            {
                Items = models,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        protected virtual IQueryable<TDbEntity> AddFilter(TSearch search, IQueryable<TDbEntity> query) => query;
        protected virtual IQueryable<TDbEntity> AddIdFilter(IQueryable<TDbEntity> query) => query;
        protected virtual IQueryable<TDbEntity> AddIncludes(IQueryable<TDbEntity> query) => query;
    }
}

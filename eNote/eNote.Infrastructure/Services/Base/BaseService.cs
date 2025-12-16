using eNote.Application;
using eNote.Application.Interfaces.Base;
using eNote.Application.SearchObjects;
using eNote.Domain.Entities.Base;
using eNote.Infrastructure.Data.Context;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Services.Base
{
    public abstract class BaseService<TModel, TSearch, TDbEntity>(ENoteContext context, IMapper mapper) : IReadService<TModel, TSearch> where TModel : class where TDbEntity : class, IEntity where TSearch : BaseSearchObject
    {
        protected readonly ENoteContext _context = context;
        protected readonly IMapper _mapper = mapper;

        public async Task<TModel> GetByIdAsync(int id)
        {
            var query = _context.Set<TDbEntity>().AsNoTracking();   

            query = AddIncludes(query);

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id) 
                ?? throw new KeyNotFoundException("ID nije pronađen");

            return _mapper.Map<TModel>(entity);
        }

        public virtual async Task<PagedResult<TModel>> GetPagedAsync(TSearch search)
        {
            var query = _context.Set<TDbEntity>().AsNoTracking().AsQueryable();

            query = AddIncludes(query);

            query = AddFilter(search, query);

            int totalCount = 0;

            if (search.IncludeTotalCount)
            { 
                totalCount = await query.CountAsync();
            }

            query = query.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize);
            
            var entities = await query.ToListAsync();

            var models = _mapper.Map<List<TModel>>(entities);

            return new PagedResult<TModel>
            {
                ResultList = models,
                Count = totalCount,
                ReturnedCount = models.Count
            };
        }

        protected virtual IQueryable<TDbEntity> AddFilter(TSearch search, IQueryable<TDbEntity> query) => query;
        protected virtual IQueryable<TDbEntity> AddIncludes(IQueryable<TDbEntity> query) => query;
    }
}

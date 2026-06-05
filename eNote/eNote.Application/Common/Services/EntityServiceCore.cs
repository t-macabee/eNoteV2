using eNote.Application.Common.Persistence;
using eNote.Application.Common.Search;
using eNote.Domain.Entities.Base;
using MapsterMapper;

namespace eNote.Application.Common.Services
{
    public abstract class EntityServiceCore<TModel, TSearch, TDbEntity>(IAppDbContext context, IMapper mapper) where TModel : class where TDbEntity : class, IEntity where TSearch : BaseSearchObject
    {
        protected readonly IAppDbContext _context = context;
        protected readonly IMapper _mapper = mapper;

        protected virtual TModel MapEntityToModel(TDbEntity entity) => _mapper.Map<TModel>(entity);
        protected virtual IQueryable<TDbEntity> AddFilter(TSearch search, IQueryable<TDbEntity> query) => query;
        protected virtual IQueryable<TDbEntity> AddIdFilter(IQueryable<TDbEntity> query) => query;
        protected virtual IQueryable<TDbEntity> AddIncludes(IQueryable<TDbEntity> query) => query;
    }
}

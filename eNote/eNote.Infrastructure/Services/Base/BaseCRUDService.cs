using eNote.Application.Interfaces.Base;
using eNote.Application.SearchObjects;
using eNote.Domain.Entities.Base;
using eNote.Infrastructure.Data.Context;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Services.Base
{
    public abstract class BaseCRUDService<TModel, TSearch, TInsert, TUpdate, TDbEntity>(ENoteContext context, IMapper mapper) 
        : BaseService<TModel, TSearch, TDbEntity>(context, mapper), ICRUDService<TModel, TSearch, TInsert, TUpdate> where TModel : class where TSearch : BaseSearchObject where TDbEntity : class, IEntity
    {
        public virtual async Task<TModel> InsertAsync(TInsert request)
        {
            var entity = _mapper.Map<TDbEntity>(request);

            await BeforeInsertAsync(request, entity);

            _context.Set<TDbEntity>().Add(entity);

            await _context.SaveChangesAsync();

            return _mapper.Map<TModel>(entity);
        }

        public virtual async Task<TModel> UpdateAsync(int id, TUpdate request)
        {
            var entity = await _context.Set<TDbEntity>().FirstOrDefaultAsync(x => x.Id == id) 
                ?? throw new KeyNotFoundException("ID nije pronađen.");

            _mapper.Map(request, entity);

            await BeforeUpdateAsync(request, entity);

            await _context.SaveChangesAsync();

            return _mapper.Map<TModel>(entity);
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _context.Set<TDbEntity>().FirstOrDefaultAsync(x => x.Id == id) 
                ?? throw new KeyNotFoundException("ID nije pronađen.");

            _context.Set<TDbEntity>().Remove(entity);

            await _context.SaveChangesAsync();
        }

        protected virtual Task BeforeInsertAsync(TInsert request, TDbEntity entity) => Task.CompletedTask;
        protected virtual Task BeforeUpdateAsync(TUpdate request, TDbEntity entity) => Task.CompletedTask;
    }
}

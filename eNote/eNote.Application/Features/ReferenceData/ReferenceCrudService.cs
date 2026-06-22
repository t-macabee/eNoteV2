using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Search;
using eNote.Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.ReferenceData;

public abstract class ReferenceCrudService<TEntity, TDto, TRequest, TSearch>(IAppDbContext context) where TEntity : BaseEntity where TSearch : BaseSearchObject
{
    protected IAppDbContext Db => context;

    protected abstract string NotFoundMessage { get; }
    protected abstract TDto Map(TEntity entity);
    protected abstract TEntity CreateEntity(TRequest request);
    protected abstract void ApplyUpdate(TEntity entity, TRequest request);
    protected abstract IOrderedQueryable<TEntity> Order(IQueryable<TEntity> query);
    protected abstract IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, TSearch search);
    protected virtual Task EnsureDeletableAsync(TEntity entity, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<PagedResult<TDto>> GetPagedAsync(TSearch search) =>
        Order(ApplySearch(Db.Set<TEntity>().AsNoTracking(), search)).ToPagedResultAsync(search, Map);

    public async Task<TDto> GetByIdAsync(int id)
    {
        var entity = await Db.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(NotFoundMessage);

        return Map(entity);
    }

    public async Task<TDto> CreateAsync(TRequest request)
    {
        var entity = CreateEntity(request);

        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<TDto> UpdateAsync(int id, TRequest request)
    {
        var entity = await Db.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(NotFoundMessage);

        ApplyUpdate(entity, request);
        await Db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await Db.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(NotFoundMessage);

        await EnsureDeletableAsync(entity);
        Db.Set<TEntity>().Remove(entity);

        await Db.SaveChangesAsync();
    }
}
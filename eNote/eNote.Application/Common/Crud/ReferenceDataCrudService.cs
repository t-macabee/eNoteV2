using eNote.Application.Common.Search;
using eNote.Domain.Entities.Shared.Base;

namespace eNote.Application.Common.Crud;

public abstract class ReferenceDataCrudService<TEntity, TDto, TRequest, TSearch>
    where TEntity : class, IEntity
    where TSearch : BaseSearchObject
{
    protected ReferenceDataCrudService(IAppDbContext context)
    {
        Db = context;
    }

    protected IAppDbContext Db { get; }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(TSearch search, CancellationToken cancellationToken = default)
    {
        var query = Db.Set<TEntity>().AsNoTracking();
        query = ApplySearch(query, search);
        query = ApplyDefaultOrder(query);

        return await query.ToPagedResultAsync(search, Map, ct: cancellationToken);
    }

    public virtual async Task<TDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        return Map(entity);
    }

    public virtual async Task<TDto> CreateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var entity = CreateEntity(request);

        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public virtual async Task<TDto> UpdateAsync(int id, TRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        UpdateEntity(entity, request);
        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        await EnsureDeletableAsync(entity, cancellationToken);

        Db.Set<TEntity>().Remove(entity);
        await Db.SaveChangesAsync(cancellationToken);
    }

    protected abstract TDto Map(TEntity entity);

    protected abstract TEntity CreateEntity(TRequest request);

    protected abstract void UpdateEntity(TEntity entity, TRequest request);

    protected abstract IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, TSearch search);

    protected abstract IOrderedQueryable<TEntity> ApplyDefaultOrder(IQueryable<TEntity> query);

    protected abstract string NotFoundMessage { get; }

    protected virtual Task EnsureDeletableAsync(TEntity entity, CancellationToken ct) => Task.CompletedTask;
}
using eNote.Application.Common.Crud;

namespace eNote.Application.Features.Communication.Events;

public sealed class EventService(IAppDbContext context) : ReferenceDataCrudService<Event, EventDto, EventRequest, EventSearchObject>(context)
{
    protected override EventDto Map(Event entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Description = entity.Description,
        StartsAt = entity.StartsAt,
        EndsAt = entity.EndsAt,
        AddressId = entity.AddressId,
        AddressStreet = entity.Address?.Street,
        AddressCity = entity.Address?.City?.Name,
        CourseId = entity.CourseId,
        CourseName = entity.Course?.Name,
        InstructorId = entity.InstructorId,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    protected override Event CreateEntity(EventRequest request) => new(
        request.Title.Trim(),
        request.Description.Trim(),
        request.StartsAt,
        request.EndsAt,
        request.AddressId,
        request.CourseId,
        request.InstructorId);

    protected override void UpdateEntity(Event entity, EventRequest request)
    {
        entity.UpdateDetails(
            request.Title.Trim(),
            request.Description.Trim(),
            request.StartsAt,
            request.EndsAt,
            request.AddressId,
            request.CourseId,
            request.InstructorId);
    }

    public override async Task<PagedResult<EventDto>> GetPagedAsync(EventSearchObject search, CancellationToken cancellationToken = default)
    {
        IQueryable<Event> query = Db.Set<Event>()
            .AsNoTracking()
            .Include(e => e.Address).ThenInclude(a => a!.City)
            .Include(e => e.Course)
            .Include(e => e.Instructor);

        query = ApplySearch(query, search);
        query = ApplyDefaultOrder(query);
        return await query.ToPagedResultAsync(search, Map, ct: cancellationToken);
    }

    public override async Task<EventDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<Event>()
            .AsNoTracking()
            .Include(e => e.Address).ThenInclude(a => a!.City)
            .Include(e => e.Course)
            .Include(e => e.Instructor)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        return Map(entity);
    }

    public override async Task<EventDto> CreateAsync(EventRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(request, cancellationToken);

        if (request.EndsAt.HasValue && request.EndsAt.Value <= request.StartsAt)
        {
            throw new BusinessException(Messages.EventEndsBeforeStarts);
        }

        return await base.CreateAsync(request, cancellationToken);
    }

    public override async Task<EventDto> UpdateAsync(int id, EventRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(request, cancellationToken);

        if (request.EndsAt.HasValue && request.EndsAt.Value <= request.StartsAt)
        {
            throw new BusinessException(Messages.EventEndsBeforeStarts);
        }

        var dto = await base.UpdateAsync(id, request, cancellationToken);

        // Re-fetch with includes for enriched navigation fields
        return await GetByIdAsync(dto.Id, cancellationToken);
    }

    protected override IQueryable<Event> ApplySearch(IQueryable<Event> query, EventSearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<Event> ApplyDefaultOrder(IQueryable<Event> query)
    {
        return query.OrderBy(x => x.StartsAt).ThenBy(x => x.Title);
    }

    protected override string NotFoundMessage => Messages.EventNotFound;

    private async Task ValidateForeignKeysAsync(EventRequest request, CancellationToken ct)
    {
        if (request.AddressId.HasValue)
        {
            var exists = await Db.Set<Address>().AnyAsync(x => x.Id == request.AddressId.Value, ct);
            if (!exists)
            {
                throw new NotFoundException(Messages.AddressNotFound);
            }
        }

        if (request.CourseId.HasValue)
        {
            var exists = await Db.Set<Course>().AnyAsync(x => x.Id == request.CourseId.Value, ct);
            if (!exists)
            {
                throw new NotFoundException(Messages.CourseNotFound);
            }
        }

        if (request.InstructorId.HasValue)
        {
            var exists = await Db.Set<Instructor>().AnyAsync(x => x.Id == request.InstructorId.Value, ct);
            if (!exists)
            {
                throw new NotFoundException(Messages.InstructorProfileNotFound);
            }
        }
    }
}

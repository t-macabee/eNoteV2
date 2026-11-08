using eNote.Application.Common.Crud;
using eNote.Application.Features.ReferenceData.Addresses;
using System.Linq.Expressions;

namespace eNote.Application.Features.Communication.Events;

public sealed class EventService(IAppDbContext context) : ReferenceDataCrudService<Event, EventDto, EventRequest, EventSearchObject>(context)
{
    protected override Expression<Func<Event, EventDto>> Projection => x => new EventDto
    {
        Id = x.Id,
        Title = x.Title,
        Description = x.Description,
        StartsAt = x.StartsAt,
        EndsAt = x.EndsAt,
        AddressId = x.AddressId,
        AddressStreet = x.Address != null ? x.Address.Street : null,
        AddressCity = x.Address != null ? x.Address.City.Name : null,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    protected override Event CreateEntity(EventRequest request) => new(
        request.Title.Trim(),
        request.Description.Trim(),
        request.StartsAt,
        request.EndsAt,
        request.AddressId);

    protected override void UpdateEntity(Event entity, EventRequest request)
    {
        entity.UpdateDetails(
            request.Title.Trim(),
            request.Description.Trim(),
            request.StartsAt,
            request.EndsAt,
            request.AddressId);
    }

    public override async Task<EventDto> CreateAsync(EventRequest request, CancellationToken cancellationToken = default)
    {
        await Db.EnsureAddressExistsAsync(request.AddressId, cancellationToken);

        return await base.CreateAsync(request, cancellationToken);
    }

    public override async Task<EventDto> UpdateAsync(int id, EventRequest request, CancellationToken cancellationToken = default)
    {
        await Db.EnsureAddressExistsAsync(request.AddressId, cancellationToken);

        return await base.UpdateAsync(id, request, cancellationToken);
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
}

using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using MapsterMapper;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalQueryService(IAppDbContext context, IMapper mapper, ICurrentActor actor, IClock clock)
{
    public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == actor.UserId), cancellationToken);

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        RentalBilling.ApplyBilling(entity, dto, clock.UtcNow);

        return dto;
    }

    public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject search, CancellationToken cancellationToken = default) => GetPagedAsync(context.Set<InstrumentRental>()
        .Where(x => x.StudentProfile.AppUserId == actor.UserId), search, cancellationToken);

    public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId), cancellationToken);

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        RentalBilling.ApplyBilling(entity, dto, clock.UtcNow);

        return dto;
    }

    public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject search, CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(context.Set<InstrumentRental>(), search, cancellationToken);
    }

    private async Task<PagedResult<InstrumentRentalDto>> GetPagedAsync(IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        return await query.AsNoTracking().WithRentalDetails().ApplySearch(search).OrderByDescending(x => x.RequestedAt).ToPagedResultAsync(search, entity =>
        {
            var dto = mapper.Map<InstrumentRentalDto>(entity);
            RentalBilling.ApplyBilling(entity, dto, now);

            return dto;
        }, ct: cancellationToken);
    }

    private static async Task<InstrumentRental> FindRentalAsync(IQueryable<InstrumentRental> query, CancellationToken cancellationToken) => await query
        .AsNoTracking().WithRentalDetails().FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.NotFound);
}

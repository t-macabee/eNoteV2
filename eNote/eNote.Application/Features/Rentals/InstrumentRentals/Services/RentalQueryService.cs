using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalQueryService(IAppDbContext context, IMapper mapper, ICurrentActor actor, IClock clock) : IRentalQueryService
{
    public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId)
    {
        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == actor.UserId));

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        RentalBilling.ApplyBilling(entity, dto, clock.UtcNow);

        return dto;
    }

    public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject search) => GetPagedAsync(context.Set<InstrumentRental>()
        .Where(x => x.StudentProfile.AppUserId == actor.UserId), search);

    public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId && x.Instrument.MusicStoreId == storeId));

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        RentalBilling.ApplyBilling(entity, dto, clock.UtcNow);

        return dto;
    }

    public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject search)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        return await GetPagedAsync(context.Set<InstrumentRental>().Where(x => x.Instrument.MusicStoreId == storeId), search);
    }

    private async Task<PagedResult<InstrumentRentalDto>> GetPagedAsync(IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search)
    {
        var now = clock.UtcNow;

        return await query.AsNoTracking().WithRentalDetails().ApplySearch(search).OrderByDescending(x => x.RequestedAt).ToPagedResultAsync(search, entity =>
        {
            var dto = mapper.Map<InstrumentRentalDto>(entity);
            RentalBilling.ApplyBilling(entity, dto, now);

            return dto;
        });
    }

    private static async Task<InstrumentRental> FindRentalAsync(IQueryable<InstrumentRental> query) => await query
        .AsNoTracking().WithRentalDetails().FirstOrDefaultAsync() ?? throw new NotFoundException(Messages.NotFound);
}

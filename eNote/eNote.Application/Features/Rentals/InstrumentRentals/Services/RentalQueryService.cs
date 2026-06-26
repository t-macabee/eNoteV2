using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.MusicStores.Services;
using eNote.Domain.Entities.Rentals;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalQueryService(
    IAppDbContext context,
    IMapper mapper,
    IMusicStoreContextService storeContext,
    ICurrentUserService currentUserService) : IRentalQueryService
{
    public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId) =>
        mapper.Map<InstrumentRentalDto>(await FindRentalAsync(context.Set<InstrumentRental>()
        .Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == currentUserService.UserId)));

    public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        return mapper.Map<InstrumentRentalDto>(await FindRentalAsync(context.Set<InstrumentRental>()
                .Where(x => x.Id == rentalId && x.Instrument.MusicStoreId == storeId)));
    }

    public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject search) =>
        GetPagedAsync(
            context.Set<InstrumentRental>()
                .Where(x => x.StudentProfile.AppUserId == currentUserService.UserId),
            search);

    public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject search)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        return await GetPagedAsync(
            context.Set<InstrumentRental>().Where(x => x.Instrument.MusicStoreId == storeId),
            search);
    }

    private async Task<PagedResult<InstrumentRentalDto>> GetPagedAsync(
        IQueryable<InstrumentRental> query,
        InstrumentRentalSearchObject search) =>
        await query
            .AsNoTracking()
            .WithRentalDetails()
            .ApplySearch(search)
            .OrderByDescending(x => x.RequestedAt)
            .ToPagedResultAsync(search, mapper.Map<InstrumentRentalDto>);

    private async Task<InstrumentRental> FindRentalAsync(IQueryable<InstrumentRental> query) =>
        await query.AsNoTracking().WithRentalDetails().FirstOrDefaultAsync()
        ?? throw new NotFoundException(Messages.NotFound);
}

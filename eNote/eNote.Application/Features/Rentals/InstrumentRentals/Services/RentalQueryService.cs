using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Common.Paging;
using MapsterMapper;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalQueryService(IAppDbContext context, IMapper mapper, ICurrentUserContext currentUser, IClock clock, IStudentDisplayNameService displayNames)
{
    public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == currentUser.UserId), cancellationToken);

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        dto.ApplyCharges(entity, entity.CalculateCharges(clock.UtcNow));

        return dto;
    }

    public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject search, CancellationToken cancellationToken = default) => GetPagedAsync(context.Set<InstrumentRental>()
        .Where(x => x.StudentProfile.AppUserId == currentUser.UserId), search, cancellationToken);

    public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId), cancellationToken);

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        dto.ApplyCharges(entity, entity.CalculateCharges(clock.UtcNow));
        dto.StudentName = await displayNames.GetStudentDisplayNameAsync(entity.StudentProfile);

        return dto;
    }

    public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject search, CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(context.Set<InstrumentRental>(), search, cancellationToken);
    }

    private async Task<PagedResult<InstrumentRentalDto>> GetPagedAsync(IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;

        var entities = await query.AsNoTracking().WithRentalDetails().ApplySearch(search)
            .OrderByDescending(x => x.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var names = await displayNames.GetStudentDisplayNamesAsync(entities.Select(e => e.StudentProfile));

        return new PagedResult<InstrumentRentalDto>
        {
            Items = [.. entities.Select(e =>
            {
                var dto = mapper.Map<InstrumentRentalDto>(e);
                dto.ApplyCharges(e, e.CalculateCharges(now));
                dto.StudentName = names.GetValueOrDefault(e.StudentProfile.Id, $"Student {e.StudentProfile.Id}");
                return dto;
            })],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static async Task<InstrumentRental> FindRentalAsync(IQueryable<InstrumentRental> query, CancellationToken cancellationToken) => await query
        .AsNoTracking().WithRentalDetails().FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.NotFound);
}

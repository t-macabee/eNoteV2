using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class AdminInstructorService(IAppDbContext context, IUserIdentityService identityService)
{
    public async Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search, CancellationToken cancellationToken = default)
    {
        IQueryable<Instructor> query = context.Set<Instructor>()
            .AsNoTracking()
            .OrderBy(x => x.Id);

        List<Instructor> instructors = await query.ToListAsync(cancellationToken);
        IReadOnlyDictionary<int, UserIdentityDto> users = await identityService.GetUsersBulkAsync(instructors.Select(x => x.AppUserId), cancellationToken);

        List<InstructorDto> mapped = [.. instructors.Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))];

        return mapped.FilterAndPage(search, search.Name, search.IsActive);
    }

    public async Task<InstructorDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Instructor entity = await context.Set<Instructor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException(Messages.InstructorProfileNotFound);

        UserIdentityDto? user = await identityService.GetUserAsync(entity.AppUserId, cancellationToken);

        return Map(entity, user);
    }

    private static InstructorDto Map(Instructor entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username,
        IsActive = user?.IsActive ?? true
    };
}

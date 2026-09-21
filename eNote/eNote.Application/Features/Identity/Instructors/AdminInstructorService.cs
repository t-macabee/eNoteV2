using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class AdminInstructorService(IAppDbContext context, IUserIdentityService identityService)
{
    public async Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search, CancellationToken cancellationToken = default)
    {
        IQueryable<Instructor> query = context.Set<Instructor>()
            .AsNoTracking();

        query = await query.WhereUserMatchesAsync(identityService, search.Name, search.IsActive, cancellationToken);

        return await query.ToPagedResultAsync(
            search,
            async (instructors, ct) =>
            {
                var users = await identityService.GetUsersBulkAsync(instructors.Select(x => x.AppUserId), ct);
                return instructors.Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)));
            },
            ct: cancellationToken);
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

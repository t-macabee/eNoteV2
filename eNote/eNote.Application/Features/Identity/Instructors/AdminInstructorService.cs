using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class AdminInstructorService(IAppDbContext context, IUserIdentityService identityService) : IAdminInstructorService
{
    public async Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search)
    {
        IQueryable<Instructor> query = context.Set<Instructor>()
            .AsNoTracking()
            .OrderBy(x => x.Id);

        List<Instructor> instructors = await query.ToListAsync();
        IReadOnlyDictionary<int, UserIdentityDto> users = await identityService.GetUsersBulkAsync(instructors.Select(x => x.AppUserId));

        List<InstructorDto> filtered = [.. instructors
            .Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))
            .Where(x => MatchesName(x, search.Name))];

        (var page, var pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        return new PagedResult<InstructorDto>
        {
            Items = [.. filtered.Skip((page - 1) * pageSize).Take(pageSize)],
            Page = page,
            PageSize = pageSize,
            TotalCount = search.IncludeTotalCount ? filtered.Count : null
        };
    }

    public async Task<InstructorDto> GetByIdAsync(int id)
    {
        Instructor entity = await context.Set<Instructor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException(Messages.InstructorProfileNotFound);

        UserIdentityDto? user = await identityService.GetUserAsync(entity.AppUserId);

        return Map(entity, user);
    }

    private static InstructorDto Map(Instructor entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username
    };

    private static bool MatchesName(InstructorDto dto, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var fullName = $"{dto.FirstName} {dto.LastName}".Trim();

        return Contains(dto.FirstName, name)
            || Contains(dto.LastName, name)
            || Contains(dto.Username, name)
            || Contains(fullName, name);
    }

    private static bool Contains(string? value, string name) => value?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
}

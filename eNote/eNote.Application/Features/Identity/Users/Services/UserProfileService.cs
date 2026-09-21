using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users.Profiles;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileService(
    IAppDbContext context,
    IUserIdentityService identity,
    IUserProfileLookup lookup,
    ICurrentUserContext currentUserService)
{
    public Task<UserProfileResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default) => GetUserAsync(currentUserService.UserId, false, cancellationToken);

    public async Task<UserProfileResponse?> GetUserAsync(int userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var user = await identity.GetUserAsync(userId, cancellationToken);

        if (user == null || (!user.IsActive && !includeInactive))
        {
            return null;
        }

        var roles = await identity.GetRolesAsync(userId, cancellationToken);

        if (roles.Count != 1)
        {
            throw new BusinessException(Messages.UserSingleRoleRequired);
        }

        var role = roles[0];

        IUserProfile profile = role switch
        {
            AppRoles.Student => await BuildStudentProfile(userId, user, cancellationToken),
            AppRoles.Instructor => await BuildInstructorProfile(userId, user, cancellationToken),
            AppRoles.StoreEmployee => await BuildMusicStoreProfile(userId, user, includeInactive, cancellationToken),
            AppRoles.Administrator => new AdminProfile(user.FirstName, user.LastName, user.DateOfBirth),
            _ => throw new BusinessException(Messages.UnknownRole)
        };

        return new UserProfileResponse(role, user.Username, user.Email, profile, user.HasPicture);
    }

    private async Task<StudentProfile> BuildStudentProfile(int userId, UserIdentityDto user, CancellationToken cancellationToken = default)
    {
        var student = await lookup.GetStudentAsync(userId, cancellationToken);

        return new StudentProfile(student.Id, student.EnrollmentDate, user.FirstName, user.LastName, user.DateOfBirth, student.MembershipPaidUntil);
    }

    private async Task<InstructorProfile> BuildInstructorProfile(int userId, UserIdentityDto user, CancellationToken cancellationToken = default)
    {
        var instructor = await lookup.GetInstructorAsync(userId, cancellationToken);

        return new InstructorProfile(instructor.Id, user.FirstName, user.LastName);
    }

    private async Task<MusicStoreProfile> BuildMusicStoreProfile(int userId, UserIdentityDto user, bool includeInactive, CancellationToken cancellationToken)
    {
        var employee = includeInactive
            ? await context.Set<MusicStoreEmployee>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == userId, cancellationToken)
                ?? throw new BusinessException(Messages.EmployeeProfileNotFound)
            : await lookup.GetActiveEmployeeAsync(userId, cancellationToken);

        var shop = await context.Set<MusicStore>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employee.MusicStoreId, cancellationToken)
            ?? throw new BusinessException(Messages.StoreNotFound);

        return new MusicStoreProfile(shop.Id, shop.StoreName, shop.BusinessHours, employee.IsManager);
    }
}

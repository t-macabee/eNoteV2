using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users.Profiles;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileService(
    IAppDbContext context,
    IUserIdentityService identity,
    IUserProfileLookup lookup,
    ICurrentUserContext currentUserService)
{
    public Task<UserProfileResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default) => GetUserAsync(currentUserService.UserId, cancellationToken);

    public async Task<UserProfileResponse?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await identity.GetUserAsync(userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        var roles = await identity.GetRolesAsync(userId);

        if (roles.Count != 1)
        {
            throw new BusinessException(Messages.UserSingleRoleRequired);
        }

        var role = roles[0];

        IUserProfile profile = role switch
        {
            AppRoles.Student => await BuildStudentProfile(userId, user),
            AppRoles.Instructor => await BuildInstructorProfile(userId, user),
            AppRoles.StoreEmployee => await BuildMusicStoreProfile(userId, user, cancellationToken),
            AppRoles.Administrator => new AdminProfile(user.FirstName, user.LastName),
            _ => throw new BusinessException(Messages.UnknownRole)
        };

        return new UserProfileResponse(role, profile);
    }

    private async Task<StudentProfile> BuildStudentProfile(int userId, UserIdentityDto user)
    {
        var student = await lookup.GetStudentAsync(userId);

        return new StudentProfile(student.Id, student.EnrollmentDate, user.FirstName, user.LastName, user.DateOfBirth, student.MembershipPaidUntil);
    }

    private async Task<InstructorProfile> BuildInstructorProfile(int userId, UserIdentityDto user)
    {
        var instructor = await lookup.GetInstructorAsync(userId);

        return new InstructorProfile(instructor.Id, user.FirstName, user.LastName);
    }

    private async Task<MusicStoreProfile> BuildMusicStoreProfile(int userId, UserIdentityDto user, CancellationToken cancellationToken)
    {
        var employee = await lookup.GetActiveEmployeeAsync(userId);

        var shop = await context.Set<MusicStore>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employee.MusicStoreId, cancellationToken)
            ?? throw new BusinessException(Messages.StoreNotFound);

        return new MusicStoreProfile(shop.Id, shop.StoreName, shop.BusinessHours);
    }
}

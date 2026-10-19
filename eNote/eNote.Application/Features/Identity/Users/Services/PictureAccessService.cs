using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class PictureAccessService(
    IAppDbContext context,
    ICurrentUserContext currentUser,
    IStoreContext stores,
    IUserIdentityService identity,
    InstructorAccessService instructorAccess) : IPictureAccessService
{
    public async Task<bool> CanViewPictureAsync(int targetAppUserId, CancellationToken cancellationToken = default)
    {
        if (targetAppUserId == currentUser.UserId)
        {
            return true;
        }

        var roles = await identity.GetRolesAsync(currentUser.UserId, cancellationToken);

        if (roles.Contains(AppRoles.Administrator))
        {
            return true;
        }

        if (roles.Contains(AppRoles.Instructor))
        {
            if (await context.Set<Instructor>().AsNoTracking()
                .AnyAsync(x => x.AppUserId == targetAppUserId, cancellationToken))
            {
                return true;
            }

            var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);

            return await context.Set<Enrollment>()
                .AsNoTracking()
                .AnyAsync(e => e.Student.AppUserId == targetAppUserId
                            && e.Course.InstructorId == instructorId
                            && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);
        }

        if (roles.Contains(AppRoles.StoreEmployee))
        {
            int ownStoreId;
            try
            {
                ownStoreId = await stores.GetCurrentStoreIdAsync(cancellationToken);
            }
            catch (StoreNotResolvedException)
            {
                return false;
            }

            var targetEmployee = await context.Set<MusicStoreEmployee>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == targetAppUserId, cancellationToken);

            if (targetEmployee is not null && targetEmployee.MusicStoreId == ownStoreId)
            {
                return true;
            }

            if (await context.Set<InstrumentRental>()
                .IgnoreQueryFilters()
                .AnyAsync(r => r.MusicStoreId == ownStoreId && r.StudentProfile.AppUserId == targetAppUserId, cancellationToken))
            {
                return true;
            }
        }

        if (roles.Contains(AppRoles.Student))
        {
            if (await context.Set<Instructor>().AsNoTracking()
                .AnyAsync(x => x.AppUserId == targetAppUserId, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }
}

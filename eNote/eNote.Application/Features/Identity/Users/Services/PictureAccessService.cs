using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Identity.Users.Services;

/// <summary>
/// Visibility rule for cross-user profile pictures (`GET users/{id}/picture`),
/// modelled on FileAccessService (same shape: role lookup, then a boolean).
/// `{id}` is always the <c>AppUser</c> id —
/// storage is keyed on <c>AppUser</c> and <c>Student.Id</c> is not
/// <c>AppUser.Id</c>, so profile ids must never be passed in.
/// </summary>
public sealed class PictureAccessService(
    IAppDbContext context,
    ICurrentUserContext currentUser,
    IStoreContext stores,
    IUserIdentityService identity,
    InstructorAccessService instructorAccess) : IPictureAccessService
{
    public async Task<bool> CanViewPictureAsync(int targetAppUserId, CancellationToken cancellationToken = default)
    {
        // Anyone may view their own picture.
        if (targetAppUserId == currentUser.UserId)
        {
            return true;
        }

        var roles = await identity.GetRolesAsync(currentUser.UserId);

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

            var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);

            return await context.Set<Enrollment>()
                .AsNoTracking()
                .AnyAsync(e => e.Student.AppUserId == targetAppUserId
                            && e.Course.InstructorId == instructorId
                            && e.EnrollmentStatus == EnrollmentStatus.Active, cancellationToken);
        }

        if (roles.Contains(AppRoles.StoreEmployee))
        {
            // Fail closed (C0.1): the tenant query filter matches everything
            // when the store is unresolved, so resolve explicitly and deny
            // when that fails — never fall through to a broader check.
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

            // Students with a rental at the viewer's own store, any status —
            // rentals are the only student↔store link in the domain.
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

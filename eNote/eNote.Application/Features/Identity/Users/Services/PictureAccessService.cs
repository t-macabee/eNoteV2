using eNote.Application.Constants;

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
    IUserIdentityService identity) : IPictureAccessService
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
            // Any other instructor, and any student with no enrolment join:
            // instructors create student profiles, so gating by the viewer's
            // own courses would hide students enrolled with a colleague.
            // Store employees: no.
            if (await context.Set<Instructor>().AsNoTracking()
                .AnyAsync(x => x.AppUserId == targetAppUserId, cancellationToken))
            {
                return true;
            }

            if (await context.Set<Student>().AsNoTracking()
                .AnyAsync(x => x.AppUserId == targetAppUserId, cancellationToken))
            {
                return true;
            }
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

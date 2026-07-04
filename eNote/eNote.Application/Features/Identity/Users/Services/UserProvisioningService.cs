using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProvisioningService(
    IAppDbContext context,
    IUserAccountService accountService,
    IUserProfileService profileService,
    IClock clock) : IUserProvisioningService
{
    public async Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        (int? UserId, string? Error) createResult = await accountService.CreateUserAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (createResult.UserId is null)
        {
            return (null, createResult.Error);
        }

        var userId = createResult.UserId.Value;

        (var Success, var Error) = await accountService.AssignSingleRoleAsync(userId, AppRoles.Student, cancellationToken);

        if (!Success)
        {
            return (null, Error);
        }

        await EnsureRoleProfileAsync(userId, AppRoles.Student, null, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var profile = await profileService.GetUserAsync(userId, cancellationToken);

        return (profile, null);
    }

    public async Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var existingUserId = await accountService.FindUserIdByUsernameAsync(username, cancellationToken);

        int userId;

        if (existingUserId.HasValue)
        {
            userId = existingUserId.Value;

            (bool Success, string? Error) updateResult = await accountService.UpdateExistingUserAsync(
                userId,
                request.Email,
                request.FirstName,
                request.LastName,
                cancellationToken: cancellationToken);

            if (!updateResult.Success)
            {
                return (userId, updateResult.Error);
            }
        }
        else
        {
            (int? UserId, string? Error) createResult = await accountService.CreateUserAsync(
                username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                cancellationToken);

            if (createResult.UserId is null)
            {
                return (0, createResult.Error);
            }

            userId = createResult.UserId.Value;
        }

        (var Success, var Error) = await accountService.AssignSingleRoleAsync(userId, request.Role, cancellationToken);

        if (!Success)
        {
            return (userId, Error);
        }

        var storeId = request.MusicStoreId ?? await ResolveDefaultStoreIdAsync(request.Role, cancellationToken);

        await EnsureRoleProfileAsync(userId, request.Role, storeId, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return (userId, null);
    }

    public async Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var student = await context.Set<Student>()
            .FirstOrDefaultAsync(s => s.AppUserId == userId, cancellationToken)
            ?? throw new NotFoundException(Messages.StudentProfileNotFound);

        student.UpdateMembership(request.PaidUntil);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int?> ResolveDefaultStoreIdAsync(string role, CancellationToken cancellationToken)
    {
        if (role != AppRoles.StoreEmployee)
        {
            return null;
        }

        return await context.Set<MusicStore>()
            .Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureRoleProfileAsync(int userId, string role, int? musicStoreId, CancellationToken cancellationToken)
    {
        switch (role)
        {
            case AppRoles.Student:
                if (!await context.Set<Student>().AnyAsync(x => x.AppUserId == userId, cancellationToken))
                {
                    context.Set<Student>().Add(new Student(userId, clock.UtcNow));
                }

                break;

            case AppRoles.Instructor:
                if (!await context.Set<Instructor>().AnyAsync(x => x.AppUserId == userId, cancellationToken))
                {
                    context.Set<Instructor>().Add(new Instructor(userId));
                }

                break;

            case AppRoles.StoreEmployee when musicStoreId.HasValue:
                {
                    var employees = await context.Set<MusicStoreEmployee>()
                        .Where(x => x.AppUserId == userId)
                        .ToListAsync(cancellationToken);

                    if (employees.Count == 0)
                    {
                        context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(userId, musicStoreId.Value, true));
                        break;
                    }

                    var primary = employees.FirstOrDefault(x => x.IsActive) ?? employees[0];
                    primary.IsActive = true;

                    foreach (MusicStoreEmployee employee in employees.Where(x => x.Id != primary.Id))
                    {
                        employee.IsActive = false;
                    }

                    break;
                }
        }
    }
}

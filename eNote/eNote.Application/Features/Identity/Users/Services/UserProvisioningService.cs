using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProvisioningService(
    IAppDbContext context,
    IUserAccountService accountService,
    IClock clock,
    IFileStorageService fileStorage,
    ICurrentUserContext currentUserContext) : IUserProvisioningService
{
    public async Task<RegistrationResult> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var userId = await ProvisionCoreAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            AppRoles.Student,
            musicStoreId: null,
            isManager: null,
            cancellationToken);

        return new RegistrationResult(userId, request.Username.Trim(), [AppRoles.Student]);
    }

    public async Task<int> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Role == AppRoles.StoreEmployee && request.MusicStoreId is null)
        {
            throw new BusinessException(Messages.MusicStoreRequiredForEmployee);
        }

        return await ProvisionCoreAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role,
            request.MusicStoreId,
            request.IsManager,
            cancellationToken);
    }

    public Task<int> ProvisionStudentByInstructorAsync(
        DelegatedUserCreateRequest request,
        CancellationToken cancellationToken = default) =>
        ProvisionCoreAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            AppRoles.Student,
            musicStoreId: null,
            isManager: null,
            cancellationToken);

    public async Task<int> ProvisionEmployeeByManagerAsync(
        DelegatedUserCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentEmployee = await UserProfileLookup.EnsureManagerAsync(context, currentUserContext.UserId, cancellationToken);

        return await ProvisionCoreAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            AppRoles.StoreEmployee,
            currentEmployee.MusicStoreId,
            isManager: false,
            cancellationToken);
    }

    private async Task<int> ProvisionCoreAsync(
        string username,
        string email,
        string password,
        string? firstName,
        string? lastName,
        string role,
        int? musicStoreId,
        bool? isManager,
        CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async () =>
        {
            var userId = await accountService.CreateUserAsync(
                username,
                email,
                password,
                firstName,
                lastName,
                cancellationToken);

            await accountService.AddRoleAsync(userId, role, cancellationToken);
            await EnsureRoleProfileAsync(userId, role, musicStoreId, cancellationToken, isManager);
            await context.SaveChangesAsync(cancellationToken);

            return userId;
        }, cancellationToken);
    }

    public async Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.AppUserId == userId && x.IsManager, cancellationToken);
    }

    public async Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!isActive && currentUserContext.UserId == userId)
        {
            throw new ConflictException(Messages.CannotModifyOwnAccount);
        }

        await accountService.SetActiveAsync(userId, isActive, cancellationToken);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (currentUserContext.UserId == userId)
        {
            throw new ConflictException(Messages.CannotModifyOwnAccount);
        }

        var picturePath = await context.ExecuteInTransactionAsync(async () =>
        {
            var storeEmployees = await context.Set<MusicStoreEmployee>()
                .Where(e => e.AppUserId == userId)
                .ToListAsync(cancellationToken);

            if (storeEmployees.Count > 0)
            {
                context.Set<MusicStoreEmployee>().RemoveRange(storeEmployees);
                await context.SaveChangesAsync(cancellationToken);
            }

            return await accountService.DeleteUserAsync(userId, cancellationToken);
        }, cancellationToken);

        if (!string.IsNullOrWhiteSpace(picturePath))
        {
            fileStorage.Delete(picturePath);
        }
    }

    private async Task EnsureRoleProfileAsync(
        int userId,
        string role,
        int? musicStoreId,
        CancellationToken cancellationToken,
        bool? isManager = null)
    {
        switch (role)
        {
            case AppRoles.Student:
                context.Set<Student>().Add(new Student(userId, clock.UtcNow));
                break;

            case AppRoles.Instructor:
                context.Set<Instructor>().Add(new Instructor(userId));
                break;

            case AppRoles.StoreEmployee when musicStoreId.HasValue:
                {
                    var isFirstEmployeeForStore = !await context.Set<MusicStoreEmployee>()
                        .IgnoreQueryFilters()
                        .AnyAsync(x => x.MusicStoreId == musicStoreId.Value, cancellationToken);

                    var managerFlag = isManager ?? isFirstEmployeeForStore;

                    context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(userId, musicStoreId.Value, managerFlag));
                    break;
                }
        }
    }
}

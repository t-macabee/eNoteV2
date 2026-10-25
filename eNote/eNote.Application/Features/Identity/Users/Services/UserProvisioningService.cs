using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProvisioningService(
    IAppDbContext context,
    IUserAccountService accountService,
    IClock clock,
    IFileStorageService fileStorage,
    ICurrentUserContext? currentUserContext = null) : IUserProvisioningService
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
        if (currentUserContext is null || !currentUserContext.IsAuthenticated)
        {
            throw new AuthorizationException(Messages.Unauthorized);
        }

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
            var normalizedUsername = username.Trim();
            var existingUserId = await accountService.FindUserIdByUsernameAsync(normalizedUsername, cancellationToken);

            if (existingUserId.HasValue)
            {
                throw new ConflictException(Messages.UsernameTaken);
            }

            var userId = await accountService.CreateUserAsync(
                normalizedUsername,
                email.Trim(),
                password,
                firstName?.Trim(),
                lastName?.Trim(),
                cancellationToken);

            await accountService.AssignSingleRoleAsync(userId, role, cancellationToken);
            await EnsureRoleProfileAsync(userId, role, musicStoreId, cancellationToken, isManager);
            await context.SaveChangesAsync(cancellationToken);

            return userId;
        }, cancellationToken);
    }

    public async Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.AppUserId == userId && x.IsActive && x.IsManager, cancellationToken);
    }

    public async Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!isActive && currentUserContext != null && currentUserContext.UserId == userId)
        {
            throw new ConflictException(Messages.CannotModifyOwnAccount);
        }

        await accountService.SetActiveAsync(userId, isActive, cancellationToken);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (currentUserContext != null && currentUserContext.UserId == userId)
        {
            throw new ConflictException(Messages.CannotModifyOwnAccount);
        }

        var hasAuthoredHistory = await context.Set<eNote.Domain.Entities.Communication.Announcement>().IgnoreQueryFilters().AnyAsync(a => a.CreatedById == userId, cancellationToken) ||
                                  await context.Set<eNote.Domain.Entities.Communication.Event>().IgnoreQueryFilters().AnyAsync(e => e.CreatedById == userId, cancellationToken) ||
                                  await context.Set<eNote.Domain.Entities.Rentals.InstrumentRental>().IgnoreQueryFilters().AnyAsync(r => r.ApprovedById == userId || r.RejectedById == userId, cancellationToken);
        if (hasAuthoredHistory) throw new ConflictException(Messages.UserDeleteBlocked);

        var student = await context.Set<Student>().FirstOrDefaultAsync(s => s.AppUserId == userId, cancellationToken);
        if (student != null)
        {
            var hasDependents = await context.Set<eNote.Domain.Entities.Academic.Enrollment>().IgnoreQueryFilters().AnyAsync(e => e.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Academic.Attendance>().IgnoreQueryFilters().AnyAsync(a => a.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Assignments.AssignmentSubmission>().IgnoreQueryFilters().AnyAsync(s => s.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Rentals.InstrumentRental>().IgnoreQueryFilters().AnyAsync(r => r.StudentProfileId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Identity.MembershipPayment>().IgnoreQueryFilters().AnyAsync(p => p.StudentId == student.Id, cancellationToken);
            if (hasDependents) throw new ConflictException(Messages.UserDeleteBlocked);

            context.Set<Student>().Remove(student);
        }

        var instructor = await context.Set<Instructor>().FirstOrDefaultAsync(i => i.AppUserId == userId, cancellationToken);
        if (instructor != null)
        {
            var hasDependents = await context.Set<eNote.Domain.Entities.Academic.Course>().IgnoreQueryFilters().AnyAsync(c => c.InstructorId == instructor.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Communication.Event>().IgnoreQueryFilters().AnyAsync(e => e.InstructorId == instructor.Id, cancellationToken);
            if (hasDependents) throw new ConflictException(Messages.UserDeleteBlocked);

            context.Set<Instructor>().Remove(instructor);
        }

        var storeEmployee = await context.Set<MusicStoreEmployee>().FirstOrDefaultAsync(e => e.AppUserId == userId, cancellationToken);
        if (storeEmployee != null)
        {
            context.Set<MusicStoreEmployee>().Remove(storeEmployee);
        }

        using var transaction = await context.BeginTransactionAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var picturePath = await accountService.DeleteUserAsync(userId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

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
                        .IgnoreQueryFilters()
                        .Where(x => x.AppUserId == userId)
                        .ToListAsync(cancellationToken);

                    if (employees.Count == 0)
                    {
                        var isFirstEmployeeForStore = !await context.Set<MusicStoreEmployee>()
                            .IgnoreQueryFilters()
                            .AnyAsync(x => x.MusicStoreId == musicStoreId.Value, cancellationToken);

                        var managerFlag = isManager ?? isFirstEmployeeForStore;

                        context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(userId, musicStoreId.Value, managerFlag));
                        break;
                    }

                    var primary = employees.FirstOrDefault(x => x.IsActive) ?? employees[0];
                    primary.IsActive = true;
                    if (isManager.HasValue)
                    {
                        primary.SetManager(isManager.Value);
                    }

                    foreach (MusicStoreEmployee employee in employees.Where(x => x.Id != primary.Id))
                    {
                        employee.IsActive = false;
                    }

                    break;
                }
        }
    }
}

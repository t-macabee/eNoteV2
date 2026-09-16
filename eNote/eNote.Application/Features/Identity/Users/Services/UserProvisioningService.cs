using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProvisioningService(
    IAppDbContext context,
    IUserAccountService accountService,
    IClock clock,
    IFileStorageService fileStorage,
    ICurrentUserContext? currentUserContext = null) : IUserProvisioningService
{
    public async Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.ExecuteInTransactionAsync(async () =>
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
                    return ((RegistrationResult?)null, createResult.Error);
                }

                var userId = createResult.UserId.Value;

                (var Success, var Error) = await accountService.AssignSingleRoleAsync(userId, AppRoles.Student, cancellationToken);

                if (!Success)
                {
                    throw new BusinessException(Error);
                }

                await EnsureRoleProfileAsync(userId, AppRoles.Student, null, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return (new RegistrationResult(userId, request.Username.Trim(), [AppRoles.Student]), (string?)null);
            }, cancellationToken);
        }
        catch (BusinessException exception)
        {
            return (null, exception.Message);
        }
    }

    public async Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.ExecuteInTransactionAsync(async () =>
            {
                if (request.Role == AppRoles.StoreEmployee && request.MusicStoreId is null)
                {
                    return (0, Messages.MusicStoreRequiredForEmployee);
                }

                var username = request.Username.Trim();
                var existingUserId = await accountService.FindUserIdByUsernameAsync(username, cancellationToken);

                if (existingUserId.HasValue)
                {
                    return (0, Messages.UsernameTaken);
                }

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

                var userId = createResult.UserId.Value;

                (var Success, var Error) = await accountService.AssignSingleRoleAsync(userId, request.Role, cancellationToken);

                if (!Success)
                {
                    throw new BusinessException(Error);
                }

                await EnsureRoleProfileAsync(userId, request.Role, request.MusicStoreId, cancellationToken, request.IsManager);

                await context.SaveChangesAsync(cancellationToken);

                return (userId, (string?)null);
            }, cancellationToken);
        }
        catch (BusinessException exception)
        {
            return (0, exception.Message);
        }
    }

    public async Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(
        DelegatedUserCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.ExecuteInTransactionAsync(async () =>
            {
                var username = request.Username.Trim();
                (int? userId, string? error) = await accountService.CreateUserAsync(
                    username,
                    request.Email.Trim(),
                    request.Password,
                    request.FirstName?.Trim(),
                    request.LastName?.Trim(),
                    cancellationToken);

                if (userId is null)
                {
                    return (0, error);
                }

                (var success, var roleError) = await accountService.AssignSingleRoleAsync(
                    userId.Value,
                    AppRoles.Student,
                    cancellationToken);

                if (!success)
                {
                    throw new BusinessException(roleError);
                }

                await EnsureRoleProfileAsync(userId.Value, AppRoles.Student, null, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                return (userId.Value, (string?)null);
            }, cancellationToken);
        }
        catch (BusinessException exception)
        {
            return (0, exception.Message);
        }
    }

    public async Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(
        DelegatedUserCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUserContext is null || !currentUserContext.IsAuthenticated)
        {
            throw new AuthorizationException(Messages.Unauthorized);
        }

        var currentEmployee = await UserProfileLookup.EnsureManagerAsync(context, currentUserContext.UserId, cancellationToken);

        var storeId = currentEmployee.MusicStoreId;

        try
        {
            return await context.ExecuteInTransactionAsync(async () =>
            {
                var username = request.Username.Trim();

                (int? userId, string? error) = await accountService.CreateUserAsync(
                    username,
                    request.Email.Trim(),
                    request.Password,
                    request.FirstName?.Trim(),
                    request.LastName?.Trim(),
                    cancellationToken);

                if (userId is null)
                {
                    return (0, error);
                }

                (var success, var roleError) = await accountService.AssignSingleRoleAsync(
                    userId.Value,
                    AppRoles.StoreEmployee,
                    cancellationToken);

                if (!success)
                {
                    throw new BusinessException(roleError);
                }

                await EnsureRoleProfileAsync(userId.Value, AppRoles.StoreEmployee, storeId, cancellationToken, isManager: false);
                await context.SaveChangesAsync(cancellationToken);

                return (userId.Value, (string?)null);
            }, cancellationToken);
        }
        catch (BusinessException exception)
        {
            return (0, exception.Message);
        }
    }

    public async Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.AppUserId == userId && x.IsActive && x.IsManager, cancellationToken);
    }

    public async Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var student = await context.Set<Student>()
            .FirstOrDefaultAsync(s => s.AppUserId == userId, cancellationToken)
            ?? throw new NotFoundException(Messages.StudentProfileNotFound);

        student.UpdateMembership(request.PaidUntil);

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!isActive && currentUserContext != null && currentUserContext.UserId == userId)
        {
            return Task.FromResult<(bool, string?)>((false, Messages.CannotModifyOwnAccount));
        }
        return accountService.SetActiveAsync(userId, isActive, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (currentUserContext != null && currentUserContext.UserId == userId)
        {
            return (false, Messages.CannotModifyOwnAccount);
        }

        bool hasAuthoredHistory = await context.Set<eNote.Domain.Entities.Communication.Announcement>().IgnoreQueryFilters().AnyAsync(a => a.CreatedById == userId, cancellationToken) ||
                                  await context.Set<eNote.Domain.Entities.Communication.Event>().IgnoreQueryFilters().AnyAsync(e => e.CreatedById == userId, cancellationToken) ||
                                  await context.Set<eNote.Domain.Entities.Rentals.InstrumentRental>().IgnoreQueryFilters().AnyAsync(r => r.ApprovedById == userId || r.RejectedById == userId, cancellationToken);
        if (hasAuthoredHistory) return (false, Messages.UserDeleteBlocked);

        var student = await context.Set<Student>().FirstOrDefaultAsync(s => s.AppUserId == userId, cancellationToken);
        if (student != null)
        {
            bool hasDependents = await context.Set<eNote.Domain.Entities.Academic.Enrollment>().IgnoreQueryFilters().AnyAsync(e => e.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Academic.Attendance>().IgnoreQueryFilters().AnyAsync(a => a.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Assignments.AssignmentSubmission>().IgnoreQueryFilters().AnyAsync(s => s.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Rentals.InstrumentRental>().IgnoreQueryFilters().AnyAsync(r => r.StudentProfileId == student.Id, cancellationToken);
            if (hasDependents) return (false, Messages.UserDeleteBlocked);
            
            context.Set<Student>().Remove(student);
        }

        var instructor = await context.Set<Instructor>().FirstOrDefaultAsync(i => i.AppUserId == userId, cancellationToken);
        if (instructor != null)
        {
            bool hasDependents = await context.Set<eNote.Domain.Entities.Academic.Course>().IgnoreQueryFilters().AnyAsync(c => c.InstructorId == instructor.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Communication.Event>().IgnoreQueryFilters().AnyAsync(e => e.InstructorId == instructor.Id, cancellationToken);
            if (hasDependents) return (false, Messages.UserDeleteBlocked);

            context.Set<Instructor>().Remove(instructor);
        }

        var storeEmployee = await context.Set<MusicStoreEmployee>().FirstOrDefaultAsync(e => e.AppUserId == userId, cancellationToken);
        if (storeEmployee != null)
        {
            context.Set<MusicStoreEmployee>().Remove(storeEmployee);
        }

        using var transaction = await context.BeginTransactionAsync(cancellationToken);
        
        await context.SaveChangesAsync(cancellationToken);
        
        var (success, error, picturePath) = await accountService.DeleteUserAsync(userId, cancellationToken);
        if (!success)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, error);
        }

        await transaction.CommitAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(picturePath))
        {
            fileStorage.Delete(picturePath);
        }

        return (true, null);
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

                        bool managerFlag = isManager ?? isFirstEmployeeForStore;

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

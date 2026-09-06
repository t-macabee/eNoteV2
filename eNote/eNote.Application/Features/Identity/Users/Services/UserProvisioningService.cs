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
    ICurrentUserContext? currentUserContext = null) : IUserProvisioningService
{
    public async Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
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

        return (new RegistrationResult(userId, request.Username.Trim(), [AppRoles.Student]), null);
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

        await EnsureRoleProfileAsync(userId, request.Role, storeId, cancellationToken, request.IsManager);

        await context.SaveChangesAsync(cancellationToken);

        return (userId, null);
    }

    public async Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(
        DelegatedUserCreateRequest request,
        CancellationToken cancellationToken = default)
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
            return (userId.Value, roleError);
        }

        await EnsureRoleProfileAsync(userId.Value, AppRoles.Student, null, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return (userId.Value, null);
    }

    public async Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(
        DelegatedUserCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUserContext is null || !currentUserContext.IsAuthenticated)
        {
            throw new AuthorizationException(Messages.Unauthorized);
        }

        var currentEmployee = await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.AppUserId == currentUserContext.UserId && x.IsActive, cancellationToken)
            ?? throw new BusinessException(Messages.EmployeeProfileNotFound);

        if (!currentEmployee.IsManager)
        {
            throw new AuthorizationException(Messages.ManagerRoleRequired);
        }

        var storeId = currentEmployee.MusicStoreId;
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
            return (userId.Value, roleError);
        }

        await EnsureRoleProfileAsync(userId.Value, AppRoles.StoreEmployee, storeId, cancellationToken, isManager: false);
        await context.SaveChangesAsync(cancellationToken);

        return (userId.Value, null);
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

    public Task<(bool Success, string? Error)> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default) =>
        accountService.SetActiveAsync(userId, false, cancellationToken);

    public Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!isActive && currentUserContext != null && currentUserContext.UserId == userId)
        {
            return Task.FromResult<(bool, string?)>((false, "Cannot deactivate your own account."));
        }
        return accountService.SetActiveAsync(userId, isActive, cancellationToken);
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

    public async Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (currentUserContext != null && currentUserContext.UserId == userId)
        {
            return (false, "Cannot delete your own account.");
        }

        var student = await context.Set<Student>().FirstOrDefaultAsync(s => s.AppUserId == userId, cancellationToken);
        if (student != null)
        {
            bool hasDependents = await context.Set<eNote.Domain.Entities.Academic.Enrollment>().AnyAsync(e => e.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Academic.Attendance>().AnyAsync(a => a.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Assignments.AssignmentSubmission>().AnyAsync(s => s.StudentId == student.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Rentals.InstrumentRental>().AnyAsync(r => r.StudentProfileId == student.Id, cancellationToken);
            if (hasDependents) return (false, Messages.UserDeleteBlocked);
            
            context.Set<Student>().Remove(student);
        }

        var instructor = await context.Set<Instructor>().FirstOrDefaultAsync(i => i.AppUserId == userId, cancellationToken);
        if (instructor != null)
        {
            bool hasDependents = await context.Set<eNote.Domain.Entities.Academic.Course>().AnyAsync(c => c.InstructorId == instructor.Id, cancellationToken) ||
                                 await context.Set<eNote.Domain.Entities.Communication.Event>().AnyAsync(e => e.InstructorId == instructor.Id, cancellationToken);
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
        
        var (success, error) = await accountService.DeleteUserAsync(userId, cancellationToken);
        if (!success)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, error);
        }

        await transaction.CommitAsync(cancellationToken);
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

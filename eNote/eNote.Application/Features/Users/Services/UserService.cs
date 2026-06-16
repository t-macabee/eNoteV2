using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Constants;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Users.Profiles;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Users.Services
{
    public class UserService(IAppDbContext context, IUserIdentityService identity, IUserAccountService accountService, IClock clock, eNote.Application.Common.Interfaces.ICurrentUserService currentUserService) : IUserService
    {
        public Task<UserProfileResponse?> GetCurrentUserAsync() => GetUserAsync(currentUserService.UserId);

        public async Task<UserProfileResponse?> GetUserAsync(int userId)
        {
            var user = await identity.GetUserAsync(userId);

            if (user == null || !user.IsActive)
                return null;

            var roles = await identity.GetRolesAsync(userId);

            if (roles.Count != 1)
                throw new BusinessException(Messages.UserSingleRoleRequired);

            var role = roles[0];

            IUserProfile profile = role switch
            {
                AppRoles.Student => await BuildStudentProfile(userId, user),
                AppRoles.Instructor => await BuildInstructorProfile(userId, user),
                AppRoles.StoreEmployee => await BuildMusicStoreProfile(userId, user),
                _ => throw new BusinessException(Messages.UnknownRole)
            };

            return new UserProfileResponse(role, profile);
        }

        public async Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request)
        {
            var createResult = await accountService.CreateUserAsync(
                request.Username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            if (createResult.UserId is null)
                return (null, createResult.Error);

            var userId = createResult.UserId.Value;

            var (Success, Error) = await accountService.AssignSingleRoleAsync(userId, AppRoles.Student);

            if (!Success)
                return (null, Error);

            await EnsureRoleProfileAsync(userId, AppRoles.Student, musicStoreId: null);

            await context.SaveChangesAsync();

            var profile = await GetUserAsync(userId);

            return (profile, null);
        }

        public async Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request)
        {
            var username = request.Username.Trim();

            var existingUserId = await accountService.FindUserIdByUsernameAsync(username);

            int userId;

            if (existingUserId.HasValue)
            {
                userId = existingUserId.Value;

                var updateResult = await accountService.UpdateExistingUserAsync(
                    userId,
                    request.Email,
                    request.FirstName,
                    request.LastName);

                if (!updateResult.Success)
                    return (userId, updateResult.Error);
            }
            else
            {
                var createResult = await accountService.CreateUserAsync(
                    username,
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName);

                if (createResult.UserId is null)
                    return (0, createResult.Error);

                userId = createResult.UserId.Value;
            }

            var (Success, Error) = await accountService.AssignSingleRoleAsync(userId, request.Role);

            if (!Success)
                return (userId, Error);

            var storeId = request.MusicStoreId ?? await ResolveDefaultStoreIdAsync(request.Role);

            await EnsureRoleProfileAsync(userId, request.Role, storeId);

            await context.SaveChangesAsync();

            return (userId, null);
        }

        private async Task<int?> ResolveDefaultStoreIdAsync(string role)
        {
            if (role != AppRoles.StoreEmployee)
                return null;

            return await context.Set<MusicStore>()
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();
        }

        private async Task EnsureRoleProfileAsync(int userId, string role, int? musicStoreId)
        {
            switch (role)
            {
                case AppRoles.Student:
                    if (!await context.Set<Student>().AnyAsync(x => x.AppUserId == userId))
                        context.Set<Student>().Add(new Student(userId, clock.UtcNow));
                    break;

                case AppRoles.Instructor:
                    if (!await context.Set<Instructor>().AnyAsync(x => x.AppUserId == userId))
                        context.Set<Instructor>().Add(new Instructor(userId));
                    break;

                case AppRoles.StoreEmployee when musicStoreId.HasValue:
                    {
                        var employees = await context.Set<MusicStoreEmployee>()
                            .Where(x => x.AppUserId == userId)
                            .ToListAsync();

                        if (employees.Count == 0)
                        {
                            context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(userId, musicStoreId.Value, true));
                            break;
                        }

                        var primary = employees.FirstOrDefault(x => x.IsActive) ?? employees[0];
                        primary.IsActive = true;

                        foreach (var employee in employees.Where(x => x.Id != primary.Id))
                            employee.IsActive = false;

                        break;
                    }
            }
        }

        private async Task<StudentProfile> BuildStudentProfile(int userId, UserIdentityDto user)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, userId);

            return new StudentProfile(
                student.Id,
                student.EnrollmentDate,
                user.FirstName,
                user.LastName,
                user.DateOfBirth,
                user.Address
            );
        }

        private async Task<InstructorProfile> BuildInstructorProfile(int userId, UserIdentityDto user)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, userId);

            return new InstructorProfile(
                instructor.Id,
                user.FirstName,
                user.LastName
            );
        }

        private async Task<MusicStoreProfile> BuildMusicStoreProfile(int userId, UserIdentityDto user)
        {
            var employee = await UserProfileHelper.GetActiveEmployeeByUserIdAsync(context, userId);

            var shop = await context.Set<MusicStore>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == employee.MusicStoreId)
                ?? throw new BusinessException(Messages.StoreNotFound);

            return new MusicStoreProfile(
                shop.Id,
                shop.StoreName,
                shop.BusinessHours,
                user.Address
            );
        }
    }
}

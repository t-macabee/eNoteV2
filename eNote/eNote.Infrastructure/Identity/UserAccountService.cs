using eNote.Application.Common.Localization;
using eNote.Application.Features.Users.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity
{
    public class UserAccountService(UserManager<AppUser> userManager) : IUserAccountService
    {
        public async Task<int?> FindUserIdByUsernameAsync(string username)
        {
            var user = await userManager.FindByNameAsync(username.Trim());

            return user?.Id;
        }

        public async Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName)
        {
            var normalizedUsername = username.Trim();

            var normalizedEmail = email.Trim();

            if (await userManager.FindByNameAsync(normalizedUsername) is not null)
                return (null, Messages.UsernameTaken);

            if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
                return (null, Messages.EmailTaken);

            var user = new AppUser
            {
                UserName = normalizedUsername,
                Email = normalizedEmail,
                EmailConfirmed = true,
                IsActive = true,
                FirstName = firstName?.Trim(),
                LastName = lastName?.Trim()
            };

            var createResult = await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));

                return (null, Messages.UserCreateFailed(normalizedUsername, errors));
            }

            return (user.Id, null);
        }

        public async Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null)
                return (false, Messages.NotFound);

            var currentRoles = await userManager.GetRolesAsync(user);

            var toRemove = currentRoles.Where(r => r != role).ToArray();

            if (toRemove.Length > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);

                if (!removeResult.Succeeded)
                {
                    var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    return (false, Messages.UserRoleRemoveFailed(user.UserName!, errors));
                }
            }

            if (!currentRoles.Contains(role))
            {
                var addResult = await userManager.AddToRoleAsync(user, role);

                if (!addResult.Succeeded)
                {
                    var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    return (false, Messages.UserRoleAssignFailed(role, user.UserName!, errors));
                }
            }

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null)
                return (false, Messages.NotFound);

            var normalizedEmail = email.Trim();

            if (user.Email != normalizedEmail)
            {
                user.Email = normalizedEmail;
                user.NormalizedEmail = userManager.NormalizeEmail(normalizedEmail);
            }

            if (!user.EmailConfirmed)
                user.EmailConfirmed = true;

            if (!user.IsActive)
                user.IsActive = true;

            user.FirstName = firstName?.Trim() ?? user.FirstName;
            user.LastName = lastName?.Trim() ?? user.LastName;

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

                return (false, Messages.UserUpdateFailed(user.UserName!, errors));
            }

            return (true, null);
        }
    }
}

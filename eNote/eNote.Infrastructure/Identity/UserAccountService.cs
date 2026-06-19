using eNote.Application.Common.Localization;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity
{
    public class UserAccountService(UserManager<AppUser> userManager) : IUserAccountService
    {
        public async Task<int?> FindUserIdByUsernameAsync(string username)
        {
            AppUser? user = await userManager.FindByNameAsync(username.Trim());

            return user?.Id;
        }

        public async Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName)
        {
            string normalizedUsername = username.Trim();
            string normalizedEmail = email.Trim();

            if (await userManager.FindByNameAsync(normalizedUsername) is not null)
            {
                return (null, Messages.UsernameTaken);
            }

            if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
            {
                return (null, Messages.EmailTaken);
            }

            AppUser user = new AppUser
            {
                UserName = normalizedUsername,
                Email = normalizedEmail,
                EmailConfirmed = true,
                IsActive = true,
                FirstName = firstName?.Trim(),
                LastName = lastName?.Trim()
            };

            IdentityResult createResult = await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                string errors = string.Join("; ", createResult.Errors.Select(e => e.Description));

                return (null, Messages.UserCreateFailed(normalizedUsername, errors));
            }

            return (user.Id, null);
        }

        public async Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role)
        {
            AppUser? user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return (false, Messages.NotFound);
            }

            IList<string> currentRoles = await userManager.GetRolesAsync(user);
            string[] toRemove = [.. currentRoles.Where(r => r != role)];

            if (toRemove.Length > 0)
            {
                IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);

                if (!removeResult.Succeeded)
                {
                    string errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    return (false, Messages.UserRoleRemoveFailed(user.UserName!, errors));
                }
            }

            if (!currentRoles.Contains(role))
            {
                IdentityResult addResult = await userManager.AddToRoleAsync(user, role);

                if (!addResult.Succeeded)
                {
                    string errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    return (false, Messages.UserRoleAssignFailed(role, user.UserName!, errors));
                }
            }

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            AppUser? user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return (false, Messages.NotFound);
            }

            IdentityResult result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName)
        {
            AppUser? user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return (false, Messages.NotFound);
            }

            string normalizedEmail = email.Trim();

            if (user.Email != normalizedEmail)
            {
                user.Email = normalizedEmail;
                user.NormalizedEmail = userManager.NormalizeEmail(normalizedEmail);
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
            }

            user.FirstName = firstName?.Trim() ?? user.FirstName;
            user.LastName = lastName?.Trim() ?? user.LastName;

            IdentityResult updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                string errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

                return (false, Messages.UserUpdateFailed(user.UserName!, errors));
            }

            return (true, null);
        }
    }
}

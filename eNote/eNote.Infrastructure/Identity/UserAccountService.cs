using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity;

public sealed class UserAccountService(UserManager<AppUser> userManager, IFileStorageService fileStorage) : IUserAccountService
{

    public async Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username.Trim());

        return user?.Id;
    }

    public async Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();
        var normalizedEmail = email.Trim();

        if (await userManager.FindByNameAsync(normalizedUsername) is not null)
        {
            return (null, Messages.UsernameTaken);
        }

        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return (null, Messages.EmailTaken);
        }

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

    public async Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var currentRoles = await userManager.GetRolesAsync(user);

        var removeError = await RemoveOtherRolesAsync(user, currentRoles, role);

        if (removeError is not null)
        {
            return (false, removeError);
        }

        var addError = await AddRoleIfMissingAsync(user, currentRoles, role);

        if (addError is not null)
        {
            return (false, addError);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        user.IsActive = isActive;

        var result = await userManager.UpdateSecurityStampAsync(user);

        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var normalizedEmail = email.Trim();

        var emailError = await ApplyEmailChangeAsync(user, normalizedEmail, userId);

        if (emailError is not null)
        {
            return (false, emailError);
        }

        ApplyProfileFields(user, firstName, lastName, dateOfBirth);

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        string picturePath;
        try
        {
            picturePath = await fileStorage.SaveAsync(picture, fileName, contentType, "profile-pictures", cancellationToken);
        }
        catch (BusinessException exception)
        {
            return (false, exception.Message);
        }

        var previousPicturePath = user.PicturePath;
        user.PicturePath = picturePath;

        var updateError = await PersistPictureChangeAsync(user, previousPicturePath, picturePath);

        if (updateError is not null)
        {
            return (false, updateError);
        }

        return (true, null);
    }

    public async Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (string.IsNullOrWhiteSpace(user?.PicturePath))
        {
            return (null, null);
        }

        return fileStorage.OpenRead(user.PicturePath);
    }

    public async Task<(bool Success, string? Error)> DeletePictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var previousPicturePath = user.PicturePath;
        user.PicturePath = null;

        var updateError = await PersistPictureChangeAsync(user, previousPicturePath);

        if (updateError is not null)
        {
            return (false, updateError);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error, string? PicturePath)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound, null);
        }

        await userManager.UpdateSecurityStampAsync(user);

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)), null);
        }

        return (true, null, user.PicturePath);
    }

    private async Task<string?> RemoveOtherRolesAsync(AppUser user, IList<string> currentRoles, string role)
    {
        string[] toRemove = [.. currentRoles.Where(r => r != role)];

        if (toRemove.Length == 0)
        {
            return null;
        }

        var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);

        if (removeResult.Succeeded)
        {
            return null;
        }

        var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
        return Messages.UserRoleRemoveFailed(user.UserName!, errors);
    }

    private async Task<string?> AddRoleIfMissingAsync(AppUser user, IList<string> currentRoles, string role)
    {
        if (currentRoles.Contains(role))
        {
            return null;
        }

        var addResult = await userManager.AddToRoleAsync(user, role);

        if (addResult.Succeeded)
        {
            return null;
        }

        var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
        return Messages.UserRoleAssignFailed(role, user.UserName!, errors);
    }

    private async Task<string?> ApplyEmailChangeAsync(AppUser user, string normalizedEmail, int userId)
    {
        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var existingWithEmail = await userManager.FindByEmailAsync(normalizedEmail);

        if (existingWithEmail is not null && existingWithEmail.Id != userId)
        {
            return Messages.EmailTaken;
        }

        user.Email = normalizedEmail;
        user.NormalizedEmail = userManager.NormalizeEmail(normalizedEmail);

        return null;
    }

    private static void ApplyProfileFields(AppUser user, string? firstName, string? lastName, DateTime? dateOfBirth)
    {
        user.FirstName = firstName?.Trim() ?? user.FirstName;
        user.LastName = lastName?.Trim() ?? user.LastName;

        if (dateOfBirth.HasValue)
        {
            user.DateOfBirth = dateOfBirth.Value.Date;
        }
    }

    private async Task<string?> PersistPictureChangeAsync(AppUser user, string? previousPicturePath, string? rollbackPath = null)
    {
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            if (rollbackPath is not null)
            {
                fileStorage.Delete(rollbackPath);
            }

            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return Messages.UserUpdateFailed(user.UserName!, errors);
        }

        if (!string.IsNullOrWhiteSpace(previousPicturePath))
        {
            fileStorage.Delete(previousPicturePath);
        }

        return null;
    }
}

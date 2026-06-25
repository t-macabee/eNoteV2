using eNote.Application.Common.Localization;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity;

public sealed class UserAccountService(UserManager<AppUser> userManager) : IUserAccountService
{
    private const int MaxPictureSizeBytes = 5 * 1024 * 1024;

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

    public async Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        string[] toRemove = [.. currentRoles.Where(r => r != role)];

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

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
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

    public async Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var normalizedEmail = email.Trim();

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existingWithEmail = await userManager.FindByEmailAsync(normalizedEmail);

            if (existingWithEmail is not null && existingWithEmail.Id != userId)
            {
                return (false, Messages.EmailTaken);
            }

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

        if (dateOfBirth.HasValue)
        {
            user.DateOfBirth = dateOfBirth.Value.Date;
        }

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, byte[] picture)
    {
        if (picture.Length == 0 || picture.Length > MaxPictureSizeBytes)
        {
            return (false, Messages.FileTooLarge);
        }

        if (!IsValidImage(picture))
        {
            return (false, Messages.InvalidFileFormat);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        user.Picture = picture;

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<(byte[]? Data, string? ContentType)> GetPictureAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.Picture is not { Length: > 0 } picture)
        {
            return (null, null);
        }

        return (picture, DetectContentType(picture));
    }

    public async Task<(bool Success, string? Error)> DeletePictureAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        user.Picture = null;

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<bool> IsAddressInUseAsync(int addressId) =>
        await userManager.Users.AnyAsync(u => u.AddressId == addressId);

    private static bool IsValidImage(byte[] data)
    {
        if (data.Length < 3)
        {
            return false;
        }

        var isJpeg = data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
        var isPng = data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
        var isWebp = data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46;

        return isJpeg || isPng || isWebp;
    }

    private static string DetectContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            return "image/png";
        }

        if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46)
        {
            return "image/webp";
        }

        return "application/octet-stream";
    }
}
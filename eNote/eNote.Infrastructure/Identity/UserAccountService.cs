using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Files;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity;

public sealed class UserAccountService(UserManager<AppUser> userManager, IFileStorageService fileStorage) : IUserAccountService
{
    private const string ClosedAccountFirstName = "Obrisani";
    private const string ClosedAccountLastName = "korisnik";
    private const string ClosedAccountEmailDomain = "enote.invalid";

    public async Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username.Trim());

        return user?.Id;
    }

    public async Task<int> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();
        var normalizedEmail = email.Trim();

        if (await userManager.FindByNameAsync(normalizedUsername) is not null)
        {
            throw new ConflictException(Messages.UsernameTaken);
        }

        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            throw new ConflictException(Messages.EmailTaken);
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

            throw new BusinessException(Messages.UserCreateFailed(normalizedUsername, errors));
        }

        return user.Id;
    }

    public async Task AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var currentRoles = await userManager.GetRolesAsync(user);

        await RemoveOtherRolesAsync(user, currentRoles, role);
        await AddRoleIfMissingAsync(user, currentRoles, role);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<bool> VerifyPasswordAsync(int userId, string currentPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        return await userManager.CheckPasswordAsync(user, currentPassword);
    }

    public async Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        user.IsActive = isActive;

        var result = await userManager.UpdateSecurityStampAsync(user);

        if (!result.Succeeded)
        {
            throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<string?> CloseAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var previousPicturePath = user.PicturePath;
        var anonymousUserName = $"obrisan-{user.Id}";

        user.FirstName = ClosedAccountFirstName;
        user.LastName = ClosedAccountLastName;
        user.UserName = anonymousUserName;
        user.NormalizedUserName = userManager.NormalizeName(anonymousUserName);
        user.Email = $"{anonymousUserName}@{ClosedAccountEmailDomain}";
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.DateOfBirth = null;
        user.PasswordHash = null;
        user.PicturePath = null;
        user.IsActive = false;

        var result = await userManager.UpdateSecurityStampAsync(user);

        if (!result.Succeeded)
        {
            throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return previousPicturePath;
    }

    public async Task UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var normalizedEmail = email.Trim();

        await ApplyEmailChangeAsync(user, normalizedEmail, userId);

        ApplyProfileFields(user, firstName, lastName, dateOfBirth);

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

            throw new BusinessException(Messages.UserUpdateFailed(user.UserName!, errors));
        }
    }

    public async Task UpdatePictureAsync(int userId, Stream picture, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var picturePath = await fileStorage.SaveAsync(picture, contentType, "profile-pictures", cancellationToken);

        var previousPicturePath = user.PicturePath;
        user.PicturePath = picturePath;

        await PersistPictureChangeAsync(user, previousPicturePath, picturePath);
    }

    public async Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (null, null);
        }

        if (string.IsNullOrWhiteSpace(user.PicturePath))
        {
            return fileStorage.OpenRead(DefaultImages.UserPicture);
        }

        return fileStorage.OpenRead(user.PicturePath);
    }

    public async Task DeletePictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        var previousPicturePath = user.PicturePath;
        user.PicturePath = null;

        await PersistPictureChangeAsync(user, previousPicturePath);
    }

    public async Task<string?> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserOrThrowAsync(userId);

        try
        {
            var result = await userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                throw new BusinessException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            throw new ConflictException(Messages.UserDeleteBlocked);
        }

        return user.PicturePath;
    }

    private async Task<AppUser> FindUserOrThrowAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user ?? throw new NotFoundException(Messages.NotFound);
    }

    private async Task RemoveOtherRolesAsync(AppUser user, IList<string> currentRoles, string role)
    {
        string[] toRemove = [.. currentRoles.Where(r => r != role)];

        if (toRemove.Length == 0)
        {
            return;
        }

        var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);

        if (!removeResult.Succeeded)
        {
            var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
            throw new BusinessException(Messages.UserRoleRemoveFailed(user.UserName!, errors));
        }
    }

    private async Task AddRoleIfMissingAsync(AppUser user, IList<string> currentRoles, string role)
    {
        if (currentRoles.Contains(role))
        {
            return;
        }

        var addResult = await userManager.AddToRoleAsync(user, role);

        if (!addResult.Succeeded)
        {
            var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
            throw new BusinessException(Messages.UserRoleAssignFailed(role, user.UserName!, errors));
        }
    }

    private async Task ApplyEmailChangeAsync(AppUser user, string normalizedEmail, int userId)
    {
        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existingWithEmail = await userManager.FindByEmailAsync(normalizedEmail);

        if (existingWithEmail is not null && existingWithEmail.Id != userId)
        {
            throw new ConflictException(Messages.EmailTaken);
        }

        user.Email = normalizedEmail;
        user.NormalizedEmail = userManager.NormalizeEmail(normalizedEmail);
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

    private async Task PersistPictureChangeAsync(AppUser user, string? previousPicturePath, string? rollbackPath = null)
    {
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            if (rollbackPath is not null)
            {
                fileStorage.Delete(rollbackPath);
            }

            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new BusinessException(Messages.UserUpdateFailed(user.UserName!, errors));
        }

        if (!string.IsNullOrWhiteSpace(previousPicturePath))
        {
            fileStorage.Delete(previousPicturePath);
        }
    }
}

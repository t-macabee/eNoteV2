namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserSelfService(
    IUserAccountService accountService,
    ICurrentUserContext currentUserService)
{
    public Task<(bool Success, string? Error)> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
        => accountService.UpdateExistingUserAsync(currentUserService.UserId, request.Email, request.FirstName, request.LastName, request.DateOfBirth, cancellationToken);

    public Task<(bool Success, string? Error)> UpdatePictureAsync(Stream picture, string fileName, string contentType, CancellationToken cancellationToken)
        => accountService.UpdatePictureAsync(currentUserService.UserId, picture, fileName, contentType, cancellationToken);

    public Task<(Stream? Data, string? ContentType)> GetPictureAsync(CancellationToken cancellationToken = default)
        => accountService.GetPictureAsync(currentUserService.UserId, cancellationToken);

    public Task<(bool Success, string? Error)> DeletePictureAsync(CancellationToken cancellationToken = default)
        => accountService.DeletePictureAsync(currentUserService.UserId, cancellationToken);

    public Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        => accountService.ChangePasswordAsync(currentUserService.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);
}

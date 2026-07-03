namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserSelfService(
    IUserAccountService accountService,
    ICurrentUserService currentUserService) : IUserSelfService
{
    public Task<(bool Success, string? Error)> UpdateProfileAsync(UpdateProfileRequest request)
        => accountService.UpdateExistingUserAsync(currentUserService.UserId, request.Email, request.FirstName, request.LastName, request.DateOfBirth);

    public Task<(bool Success, string? Error)> UpdatePictureAsync(byte[] picture)
        => accountService.UpdatePictureAsync(currentUserService.UserId, picture);

    public Task<(byte[]? Data, string? ContentType)> GetPictureAsync()
        => accountService.GetPictureAsync(currentUserService.UserId);

    public Task<(bool Success, string? Error)> DeletePictureAsync()
        => accountService.DeletePictureAsync(currentUserService.UserId);

    public Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordRequest request)
        => accountService.ChangePasswordAsync(currentUserService.UserId, request.CurrentPassword, request.NewPassword);
}

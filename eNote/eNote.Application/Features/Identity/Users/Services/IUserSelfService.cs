namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserSelfService
{
    Task<(bool Success, string? Error)> UpdateProfileAsync(UpdateProfileRequest request);
    Task<(bool Success, string? Error)> UpdatePictureAsync(Stream picture, string fileName, string contentType);
    Task<(Stream? Data, string? ContentType)> GetPictureAsync();
    Task<(bool Success, string? Error)> DeletePictureAsync();
    Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordRequest request);
}

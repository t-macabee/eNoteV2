namespace eNote.Application.Features.Identity.Users.Services;

public interface IPictureAccessService
{
    Task<bool> CanViewPictureAsync(int targetAppUserId, CancellationToken cancellationToken = default);
}

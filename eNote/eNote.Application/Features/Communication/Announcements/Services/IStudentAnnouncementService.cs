namespace eNote.Application.Features.Communication.Announcements.Services;

public interface IStudentAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search, CancellationToken cancellationToken = default);
}

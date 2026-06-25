using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Announcements.Services;

public interface IStudentAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search);
}

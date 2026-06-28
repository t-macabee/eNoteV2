using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Announcements;

namespace eNote.Application.Features.Communication.Announcements.Services;

public interface ICourseAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request, CancellationToken cancellationToken = default);
    Task DeleteForCourseAsync(int courseId, int announcementId, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

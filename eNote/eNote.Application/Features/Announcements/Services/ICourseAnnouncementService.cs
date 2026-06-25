using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Announcements.Services;

public interface ICourseAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search);
    Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId);
    Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request);
    Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request);
    Task DeleteForCourseAsync(int courseId, int announcementId);
    Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

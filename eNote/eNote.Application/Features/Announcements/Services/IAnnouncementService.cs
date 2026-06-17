using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Announcements.Services
{
    public interface IAnnouncementService
    {
        Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(int page, int pageSize);
        Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request);
        Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId);
        Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, int page, int pageSize);
        Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request);
        Task DeleteForCourseAsync(int courseId, int announcementId);
        Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request);
        Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId);
        Task<PagedResult<AnnouncementDto>> GetForStoreAsync(int page, int pageSize);
        Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request);
        Task DeleteForStoreAsync(int announcementId);
    }
}

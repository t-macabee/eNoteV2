using eNote.Application.Features.Announcements.DTOs;
using eNote.Application.Features.Announcements.Requests;

namespace eNote.Application.Features.Announcements.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<IReadOnlyList<AnnouncementDto>> GetFeedForStudentAsync(int userId);
        Task<AnnouncementDto> CreateForCourseAsync(int userId, int courseId, AnnouncementCreateRequest request);
        Task<IReadOnlyList<AnnouncementDto>> GetForCourseAsync(int userId, int courseId);
        Task<AnnouncementDto> CreateForStoreAsync(int userId, AnnouncementCreateRequest request);
        Task<IReadOnlyList<AnnouncementDto>> GetForStoreAsync(int userId);
    }
}

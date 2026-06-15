namespace eNote.Application.Features.Announcements.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<IReadOnlyList<AnnouncementDto>> GetFeedForStudentAsync(int userId);
        Task<AnnouncementDto> CreateForCourseAsync(int userId, int courseId, AnnouncementCreateRequest request);
        Task<IReadOnlyList<AnnouncementDto>> GetForCourseAsync(int userId, int courseId);
        Task<AnnouncementDto> UpdateForCourseAsync(int userId, int courseId, int announcementId, AnnouncementUpdateRequest request);
        Task DeleteForCourseAsync(int userId, int courseId, int announcementId);
        Task<AnnouncementDto> CreateForStoreAsync(int userId, AnnouncementCreateRequest request);
        Task<IReadOnlyList<AnnouncementDto>> GetForStoreAsync(int userId);
        Task<AnnouncementDto> UpdateForStoreAsync(int userId, int announcementId, AnnouncementUpdateRequest request);
        Task DeleteForStoreAsync(int userId, int announcementId);
    }
}

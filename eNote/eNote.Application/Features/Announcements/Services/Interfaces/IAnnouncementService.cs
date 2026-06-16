namespace eNote.Application.Features.Announcements.Services
{
    public interface IAnnouncementService
    {
        Task<IReadOnlyList<AnnouncementDto>> GetFeedForStudentAsync();
        Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request);
        Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId);
        Task<IReadOnlyList<AnnouncementDto>> GetForCourseAsync(int courseId);
        Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request);
        Task DeleteForCourseAsync(int courseId, int announcementId);
        Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request);
        Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId);
        Task<IReadOnlyList<AnnouncementDto>> GetForStoreAsync();
        Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request);
        Task DeleteForStoreAsync(int announcementId);
    }
}

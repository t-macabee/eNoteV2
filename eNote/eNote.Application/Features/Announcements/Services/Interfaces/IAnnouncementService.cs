namespace eNote.Application.Features.Announcements.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<IReadOnlyList<AnnouncementDto>> GetFeedForStudentAsync();
        Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementCreateRequest request);
        Task<IReadOnlyList<AnnouncementDto>> GetForCourseAsync(int courseId);
        Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementUpdateRequest request);
        Task DeleteForCourseAsync(int courseId, int announcementId);
        Task<AnnouncementDto> CreateForStoreAsync(AnnouncementCreateRequest request);
        Task<IReadOnlyList<AnnouncementDto>> GetForStoreAsync();
        Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementUpdateRequest request);
        Task DeleteForStoreAsync(int announcementId);
    }
}

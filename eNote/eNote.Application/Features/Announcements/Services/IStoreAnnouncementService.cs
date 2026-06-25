using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Announcements.Services;

public interface IStoreAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetForStoreAsync(AnnouncementSearchObject search);
    Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId);
    Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request);
    Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request);
    Task DeleteForStoreAsync(int announcementId);
    Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

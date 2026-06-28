using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Announcements;

namespace eNote.Application.Features.Communication.Announcements.Services;

public interface IStoreAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetForStoreAsync(AnnouncementSearchObject search, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request, CancellationToken cancellationToken = default);
    Task DeleteForStoreAsync(int announcementId, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

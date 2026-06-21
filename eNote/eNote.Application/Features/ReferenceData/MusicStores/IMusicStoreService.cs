using eNote.Application.Common.Paging;

namespace eNote.Application.Features.ReferenceData.MusicStores;

public interface IMusicStoreService
{
    Task<PagedResult<MusicStoreDto>> GetPagedAsync(int page, int pageSize);
    Task<MusicStoreDto> GetByIdAsync(int id);
    Task<MusicStoreDto> CreateAsync(MusicStoreRequest request);
    Task<MusicStoreDto> UpdateAsync(int id, MusicStoreRequest request);
    Task DeleteAsync(int id);
}

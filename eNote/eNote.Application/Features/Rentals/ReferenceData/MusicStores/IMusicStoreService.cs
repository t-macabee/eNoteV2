namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public interface IMusicStoreService
{
    Task<PagedResult<MusicStoreDto>> GetPagedAsync(MusicStoreSearchObject search, CancellationToken cancellationToken = default);
    Task<MusicStoreDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MusicStoreDto> CreateAsync(MusicStoreRequest request, CancellationToken cancellationToken = default);
    Task<MusicStoreDto> UpdateAsync(int id, MusicStoreRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

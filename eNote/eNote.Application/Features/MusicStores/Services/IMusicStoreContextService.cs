namespace eNote.Application.Features.MusicStores.Services;

public interface IMusicStoreContextService
{
    Task<int> GetActiveStoreAsync(int appUserId, CancellationToken ct = default);
}

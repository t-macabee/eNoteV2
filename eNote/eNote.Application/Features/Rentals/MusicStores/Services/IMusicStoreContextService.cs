namespace eNote.Application.Features.Rentals.MusicStores.Services;

public interface IMusicStoreContextService
{
    Task<int> GetActiveStoreAsync(int appUserId, CancellationToken ct = default);
}

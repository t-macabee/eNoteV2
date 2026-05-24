namespace eNote.Application.Features.MusicStores.Services.Interfaces
{
    public interface IMusicStoreContextService
    {
        Task<int> GetActiveStoreAsync(int appUserId, CancellationToken ct = default);
    }
}

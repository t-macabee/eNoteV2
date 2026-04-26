namespace eNote.Application.Features.MusicStores.Context.Services
{
    public interface IMusicStoreContextService
    {
        Task<int> GetActiveStoreAsync(int appUserId, CancellationToken ct = default);
    }
}

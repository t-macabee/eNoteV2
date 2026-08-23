namespace eNote.Application.Common.Interfaces;

public interface IStoreContext
{
    Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default);
}

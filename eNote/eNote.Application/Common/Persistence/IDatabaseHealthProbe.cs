namespace eNote.Application.Common.Persistence;

public interface IDatabaseHealthProbe
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}

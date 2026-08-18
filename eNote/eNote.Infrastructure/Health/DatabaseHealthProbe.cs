using eNote.Application.Common.Persistence;
using eNote.Infrastructure.Data;

namespace eNote.Infrastructure.Health;

public sealed class DatabaseHealthProbe(ENoteContext dbContext) : IDatabaseHealthProbe
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        => dbContext.Database.CanConnectAsync(cancellationToken);
}

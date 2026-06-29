using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data;

public sealed class MigrationRunner(ENoteContext context) : IMigrationRunner
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
    }
}

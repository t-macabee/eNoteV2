namespace eNote.Application.Common.Persistence;

public interface IMigrationRunner
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

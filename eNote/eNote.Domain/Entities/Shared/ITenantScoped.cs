// hook-test: trivial edit to verify pre-commit indexer hook, will be reverted
namespace eNote.Domain.Entities.Shared;

public interface ITenantScoped
{
    int MusicStoreId { get; }
}
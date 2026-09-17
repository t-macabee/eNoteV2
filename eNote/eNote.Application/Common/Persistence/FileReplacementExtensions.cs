namespace eNote.Application.Common.Persistence;

public static class FileReplacementExtensions
{
    /// <summary>
    /// Persists an entity change that points at a freshly stored file. If the save fails the new
    /// file is removed so no orphan stays on disk; once it commits, the file it replaced is removed.
    /// </summary>
    public static async Task SaveChangesReplacingFileAsync(this IAppDbContext db, IFileStorageService fileStorage, string newPath, string? previousPath, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            fileStorage.Delete(newPath);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousPath))
        {
            fileStorage.Delete(previousPath);
        }
    }
}

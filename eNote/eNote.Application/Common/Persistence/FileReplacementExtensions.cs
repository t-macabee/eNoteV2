namespace eNote.Application.Common.Persistence;

public static class FileReplacementExtensions
{
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

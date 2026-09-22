namespace eNote.Application.Common.Files;

public static class StoreStorageQuota
{
    public static async Task EnsureWithinQuotaAsync(
        IAppDbContext db,
        IFileStorageService fileStorage,
        int musicStoreId,
        long incomingBytes,
        string? previousPath,
        CancellationToken ct = default)
    {
        var paths = await db.Set<Instrument>()
            .IgnoreQueryFilters()
            .Where(x => x.MusicStoreId == musicStoreId && x.ImagePath != null)
            .Select(x => x.ImagePath!)
            .ToListAsync(ct);

        var storeImagePath = await db.Set<MusicStore>()
            .Where(x => x.Id == musicStoreId)
            .Select(x => x.ImagePath)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(storeImagePath))
        {
            paths.Add(storeImagePath);
        }

        var usedBytes = paths
            .Where(path => !string.Equals(path, previousPath, StringComparison.Ordinal))
            .Sum(fileStorage.GetFileSize);

        if (usedBytes + incomingBytes > FileUploadLimits.MaxStoreUploadBytes)
        {
            throw new BusinessException(Messages.StorageQuotaExceeded);
        }
    }
}

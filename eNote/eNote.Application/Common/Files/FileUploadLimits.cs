namespace eNote.Application.Common.Files;

public static class FileUploadLimits
{
    public const long MaxFileBytes = 5 * 1024 * 1024;

    public const long MaxRequestBytes = MaxFileBytes + 1024 * 1024;

    public const long MaxStoreUploadBytes = 50 * 1024 * 1024;
}

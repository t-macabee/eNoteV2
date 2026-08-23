namespace eNote.Infrastructure.Storage;

internal static class FileSignatureDetector
{
    internal const string JpegMimeType = "image/jpeg";
    internal const string PngMimeType = "image/png";
    internal const string WebpMimeType = "image/webp";
    internal const string PdfMimeType = "application/pdf";

    internal static string DetectContentType(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return JpegMimeType;
        }

        if (data.Length >= 8
            && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
            && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            return PngMimeType;
        }

        if (data.Length >= 12
            && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
            && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
        {
            return WebpMimeType;
        }

        if (data.Length >= 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
        {
            return PdfMimeType;
        }

        return "application/octet-stream";
    }

    internal static bool IsAllowed(ReadOnlySpan<byte> data, string[] allowedContentTypes) =>
        allowedContentTypes.Contains(DetectContentType(data), StringComparer.OrdinalIgnoreCase);
}

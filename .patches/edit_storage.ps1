$path = 'C:\Users\Tarik\Desktop\eNoteV2\eNote\eNote.Infrastructure\Storage\LocalFileStorageService.cs'
$content = [System.IO.File]::ReadAllText($path)
$old = 'var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
        {
            ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "application/pdf" => ".pdf",
                _ => ".bin"
            };
        }'
$new = 'var ext = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => throw new BusinessException(Messages.InvalidFileFormat)
        };'
if ($content.Contains($old)) { $content = $content.Replace($old, $new); [System.IO.File]::WriteAllText($path, $content); Write-Output 'STORAGE REPLACED' } else { Write-Output 'STORAGE OLD BLOCK NOT FOUND' }

$path = 'C:\Users\Tarik\Desktop\eNoteV2\eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\RentalCommandService.cs'
$content = [System.IO.File]::ReadAllText($path)
$old = 'catch (DbUpdateException)
        {
            throw new BusinessException(message);
        }'
$new = 'catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_InstrumentRentals_InstrumentId") == true)
        {
            throw new BusinessException(message);
        }'
if ($content.Contains($old)) { $content = $content.Replace($old, $new); [System.IO.File]::WriteAllText($path, $content); Write-Output 'RENTAL REPLACED' } else { Write-Output 'RENTAL OLD BLOCK NOT FOUND' }

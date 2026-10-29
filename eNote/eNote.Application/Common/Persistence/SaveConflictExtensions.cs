using eNote.Application.Constants;

namespace eNote.Application.Common.Persistence;

public static class SaveConflictExtensions
{
    public static async Task SaveChangesMappingConflictAsync(this IAppDbContext db, string message, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.InstrumentRentalActiveOrApprovedUniqueIndex))
        {
            throw new BusinessException(message);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.InstrumentRentalPendingUniqueIndex))
        {
            throw new BusinessException(Messages.RentalPendingRequired);
        }
    }
}

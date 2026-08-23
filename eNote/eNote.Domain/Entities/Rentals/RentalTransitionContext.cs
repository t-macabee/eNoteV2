namespace eNote.Domain.Entities.Rentals;

public sealed record RentalTransitionContext
{
    public required int UserId { get; init; }
    public required RentalActor Actor { get; init; }
    public required bool HasInstrumentLockConflict { get; init; }
    public required decimal MonthlyFee { get; init; }
    public required bool IsInstrumentActive { get; init; }

    /// <summary>
    /// The note text from the caller's <c>RentalStatusResponse</c> (an Application-layer DTO not
    /// visible to Domain), flattened to the one field the transition logic actually reads.
    /// </summary>
    public string? ResponseNote { get; init; }
}

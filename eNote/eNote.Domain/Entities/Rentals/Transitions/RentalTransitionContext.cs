namespace eNote.Domain.Entities.Rentals.Transitions;

public sealed record RentalTransitionContext
{
    public required int? UserId { get; init; }
    public required RentalActor Actor { get; init; }
    public required bool HasInstrumentLockConflict { get; init; }
    public required bool IsInstrumentInactive { get; init; }
    public required decimal MonthlyFee { get; init; }
    public required decimal OverduePenaltyMultiplier { get; init; }

    public string? ResponseNote { get; init; }
}

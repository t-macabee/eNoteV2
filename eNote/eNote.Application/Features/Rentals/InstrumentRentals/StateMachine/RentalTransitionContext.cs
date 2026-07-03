namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed record RentalTransitionContext
{
    public required int UserId { get; init; }
    public required RentalActor Actor { get; init; }
    public required bool HasInstrumentLockConflict { get; init; }
    public required decimal MonthlyFee { get; init; }
    public required bool IsInstrumentActive { get; init; }
    public RentalStatusResponse? Response { get; init; }
}

using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed class RentalTransitionContext
{
    public required int UserId { get; init; }
    public required RentalActor Actor { get; init; }
    public required IAppDbContext Db { get; init; }

    public RentalStatusResponse? Response { get; init; }
}

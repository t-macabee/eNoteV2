using eNote.Application.Common.Persistence;

namespace eNote.Application.Features.InstrumentRentals.StateMachine
{
    public sealed class RentalTransitionContext
    {
        public required int UserId { get; init; }
        public required RentalActor Actor { get; init; }
        public required IAppDbContext Db { get; init; }

        public RentalStatusResponse? Response { get; init; }
    }
}

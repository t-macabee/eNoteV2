namespace eNote.Application.Features.InstrumentRentals.StateMachine;

public enum RentalTrigger
{
    Approve,
    Reject,
    Pickup,
    Complete,
    Cancel,
    ReturnEarly
}

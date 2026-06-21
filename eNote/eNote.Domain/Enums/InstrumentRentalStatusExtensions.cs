namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusExtensions
{
    public static bool BlocksInstrument(this InstrumentRentalStatus status) =>
        status is InstrumentRentalStatus.Approved or InstrumentRentalStatus.Active;

    public static bool IsBillingEligible(this InstrumentRentalStatus status) =>
        status is InstrumentRentalStatus.Active
            or InstrumentRentalStatus.Completed
            or InstrumentRentalStatus.ReturnedEarly;
}
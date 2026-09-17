namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusExtensions
{
    public static readonly InstrumentRentalStatus[] BlockingStatuses =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active
    ];

    public static bool BlocksInstrument(this InstrumentRentalStatus status) =>
        BlockingStatuses.Contains(status);

    public static bool IsBillingEligible(this InstrumentRentalStatus status) =>
        status is InstrumentRentalStatus.Active
            or InstrumentRentalStatus.Completed
            or InstrumentRentalStatus.ReturnedEarly;
}
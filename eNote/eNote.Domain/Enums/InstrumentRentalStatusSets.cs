namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusSets
{
    public static readonly InstrumentRentalStatus[] Blocking =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active
    ];

    public static readonly InstrumentRentalStatus[] History =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    ];
}
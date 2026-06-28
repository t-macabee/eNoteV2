using System.Collections.Frozen;

namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusSets
{
    public static readonly FrozenSet<InstrumentRentalStatus> Blocking = new InstrumentRentalStatus[]
    {
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active
    }.ToFrozenSet();

    public static readonly FrozenSet<InstrumentRentalStatus> History = new InstrumentRentalStatus[]
    {
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    }.ToFrozenSet();
}
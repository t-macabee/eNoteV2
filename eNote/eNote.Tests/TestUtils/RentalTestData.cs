namespace eNote.Tests.TestUtils;

public static class RentalTestData
{
    public static ENoteContext CreateContext(DateTime now) =>
        new(TestDbContextFactory.CreateOptions(), new FixedClock(now), new StubCurrentActor(storeId: 1)) { ExplicitStoreId = 1 };

    public static async Task<Instrument> SeedInstrumentAsync(ENoteContext context)
    {
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    public static InstrumentRental CreateCompletedRental(Instrument instrument, int studentId, DateTime now)
    {
        var rental = new InstrumentRental(instrument.Id, studentId, instrument.MusicStoreId, now.AddDays(-33), null);
        AdvanceTo(
            rental,
            InstrumentRentalStatus.Completed,
            now,
            approvedAt: now.AddDays(-32),
            pickedUpAt: now.AddDays(-31),
            finishedAt: now.AddDays(-1));
        return rental;
    }

    /// <summary>
    /// Moves a rental to <paramref name="target"/> through the real transition table, one step at a time,
    /// with the actor each step allows. Use it instead of the entity's state methods so tests cannot
    /// create a state the rental rules do not allow.
    /// </summary>
    public static void AdvanceTo(
        InstrumentRental rental,
        InstrumentRentalStatus target,
        DateTime now,
        decimal fee = 50m,
        decimal overduePenaltyMultiplier = 0.5m,
        string? note = null,
        int userId = 1,
        DateTime? approvedAt = null,
        DateTime? pickedUpAt = null,
        DateTime? finishedAt = null)
    {
        var approvedTime = approvedAt ?? now;
        var pickedUpTime = pickedUpAt ?? approvedTime;
        var finishedTime = finishedAt ?? pickedUpTime;

        void Step(RentalTrigger trigger, RentalActor actor, DateTime at, string? stepNote)
        {
            var context = new RentalTransitionContext
            {
                UserId = userId,
                Actor = actor,
                HasInstrumentLockConflict = false,
                IsInstrumentInactive = rental.Instrument is { IsActive: false },
                MonthlyFee = fee,
                OverduePenaltyMultiplier = overduePenaltyMultiplier,
                ResponseNote = stepNote
            };

            var result = rental.Transition(trigger, context, at);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Cannot move rental from {rental.RentalStatus} with {trigger} as {actor}: {result.Error}");
            }
        }

        switch (target)
        {
            case InstrumentRentalStatus.Pending:
                return;
            case InstrumentRentalStatus.Approved:
                Step(RentalTrigger.Approve, RentalActor.StoreEmployee, approvedTime, note);
                return;
            case InstrumentRentalStatus.Active:
                Step(RentalTrigger.Approve, RentalActor.StoreEmployee, approvedTime, note);
                Step(RentalTrigger.Pickup, RentalActor.StoreEmployee, pickedUpTime, note);
                return;
            case InstrumentRentalStatus.Completed:
                Step(RentalTrigger.Approve, RentalActor.StoreEmployee, approvedTime, note);
                Step(RentalTrigger.Pickup, RentalActor.StoreEmployee, pickedUpTime, note);
                Step(RentalTrigger.Complete, RentalActor.StoreEmployee, finishedTime, note);
                return;
            case InstrumentRentalStatus.ReturnedEarly:
                Step(RentalTrigger.Approve, RentalActor.StoreEmployee, approvedTime, note);
                Step(RentalTrigger.Pickup, RentalActor.StoreEmployee, pickedUpTime, note);
                Step(RentalTrigger.ReturnEarly, RentalActor.StoreEmployee, finishedTime, note);
                return;
            case InstrumentRentalStatus.Rejected:
                Step(RentalTrigger.Reject, RentalActor.StoreEmployee, now, note ?? "Razlog.");
                return;
            case InstrumentRentalStatus.Canceled:
                Step(RentalTrigger.Cancel, RentalActor.Student, now, note ?? "Test note.");
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported rental status.");
        }
    }
}

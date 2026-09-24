using eNote.Application.Common.Time;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Entities.Rentals.Transitions;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

internal static class RentalActivitySeed
{
    public static async Task SeedRentals(ENoteContext context, IClock clock)
    {
        if (await context.Set<InstrumentRental>().IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        var defaultStoreId = await StoreSeed.EnsureDefaultStoreAsync(context);
        var storeEmployeeUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "storeemployee");
        if (storeEmployeeUser == null)
        {
            return;
        }

        var students = await SeedStudents.GetByUsernameAsync(context);

        var instruments = await context.Set<Instrument>()
            .Include(i => i.InstrumentType)
            .Where(i => i.MusicStoreId == defaultStoreId)
            .ToListAsync();
        var instrumentMap = instruments.ToDictionary(i => i.Model, i => i);

        var now = clock.UtcNow;
        var rentals = new List<InstrumentRental>();

        InstrumentRental Request(string username, string model, int daysAgo)
        {
            var rental = new InstrumentRental(instrumentMap[model].Id, students[username].Id, defaultStoreId, now.AddDays(-daysAgo), null);
            rentals.Add(rental);
            return rental;
        }

        var feeByInstrumentId = instruments.ToDictionary(i => i.Id, i => i.InstrumentType.MonthlyFee);
        decimal FeeOf(InstrumentRental rental) => feeByInstrumentId[rental.InstrumentId];

        void Store(InstrumentRental rental, RentalTrigger trigger, int daysAgo, string? note = null) =>
            ApplyStep(rental, trigger, storeEmployeeUser.Id, RentalActor.StoreEmployee, FeeOf(rental), now.AddDays(-daysAgo), note);

        void StudentCancel(InstrumentRental rental, string username, int daysAgo, string note) =>
            ApplyStep(rental, RentalTrigger.Cancel, students[username].AppUserId, RentalActor.Student, FeeOf(rental), now.AddDays(-daysAgo), note);

        // Pays exactly what the rental charges at its return (days × monthly fee / 30).
        void Pay(InstrumentRental rental, int daysAgo) =>
            rental.MarkPaid((long)(rental.CalculateCharges(rental.ReturnedAt!.Value).TotalFee!.Value * 100), now.AddDays(-daysAgo));

        // 1. student | Stratocaster | Completed, paid
        var r1 = Request("student", "Stratocaster", 50);
        Store(r1, RentalTrigger.Approve, 49);
        Store(r1, RentalTrigger.Pickup, 48);
        Store(r1, RentalTrigger.Complete, 20);
        Pay(r1, 19);

        // 2. student | Precision Bass | ReturnedEarly, paid
        var r2 = Request("student", "Precision Bass", 40);
        Store(r2, RentalTrigger.Approve, 39);
        Store(r2, RentalTrigger.Pickup, 38);
        Store(r2, RentalTrigger.ReturnEarly, 30);
        Pay(r2, 29);

        // 3. student | Les Paul | Active
        var r3 = Request("student", "Les Paul", 12);
        Store(r3, RentalTrigger.Approve, 11);
        Store(r3, RentalTrigger.Pickup, 10);

        // 4. student | Minilogue | Pending
        Request("student", "Minilogue", 1);

        // 5. student | Export | Rejected
        var r5 = Request("student", "Export", 15);
        Store(r5, RentalTrigger.Reject, 14, "Set je rezervisan za školski koncert.");

        // 6. student | YAS-280 | Canceled
        var r6 = Request("student", "YAS-280", 9);
        StudentCancel(r6, "student", 8, "Više mi ne treba.");

        // 7. mobile | Pacifica | Completed, not paid
        var r7 = Request("mobile", "Pacifica", 25);
        Store(r7, RentalTrigger.Approve, 24);
        Store(r7, RentalTrigger.Pickup, 23);
        Store(r7, RentalTrigger.Complete, 3);

        // 8. mobile | Custom 24 | Approved
        var r8 = Request("mobile", "Custom 24", 2);
        Store(r8, RentalTrigger.Approve, 1);

        // 9. student1 | Stratocaster | Completed, paid
        var r9 = Request("student1", "Stratocaster", 90);
        Store(r9, RentalTrigger.Approve, 89);
        Store(r9, RentalTrigger.Pickup, 88);
        Store(r9, RentalTrigger.Complete, 60);
        Pay(r9, 59);

        // 10. student1 | Blues Junior IV | Completed, paid
        var r10 = Request("student1", "Blues Junior IV", 45);
        Store(r10, RentalTrigger.Approve, 44);
        Store(r10, RentalTrigger.Pickup, 43);
        Store(r10, RentalTrigger.Complete, 15);
        Pay(r10, 14);

        // 11. student1 | RG | Active
        var r11 = Request("student1", "RG", 7);
        Store(r11, RentalTrigger.Approve, 6);
        Store(r11, RentalTrigger.Pickup, 5);

        // 12. student2 | Stratocaster | Completed, paid
        var r12 = Request("student2", "Stratocaster", 18);
        Store(r12, RentalTrigger.Approve, 17);
        Store(r12, RentalTrigger.Pickup, 16);
        Store(r12, RentalTrigger.Complete, 6);
        Pay(r12, 5);

        // 13. student2 | D-28 | ReturnedEarly, paid
        var r13 = Request("student2", "D-28", 35);
        Store(r13, RentalTrigger.Approve, 34);
        Store(r13, RentalTrigger.Pickup, 33);
        Store(r13, RentalTrigger.ReturnEarly, 28);
        Pay(r13, 27);

        // 14. student2 | Juno-DS | Pending
        Request("student2", "Juno-DS", 2);

        // 15. student3 | Precision Bass | Completed, paid
        var r15 = Request("student3", "Precision Bass", 70);
        Store(r15, RentalTrigger.Approve, 69);
        Store(r15, RentalTrigger.Pickup, 68);
        Store(r15, RentalTrigger.Complete, 45);
        Pay(r15, 44);

        // 16. student3 | Les Paul | Completed, paid
        var r16 = Request("student3", "Les Paul", 40);
        Store(r16, RentalTrigger.Approve, 39);
        Store(r16, RentalTrigger.Pickup, 38);
        Store(r16, RentalTrigger.Complete, 14);
        Pay(r16, 13);

        // 17. student3 | Thunderbird | Rejected
        var r17 = Request("student3", "Thunderbird", 5);
        Store(r17, RentalTrigger.Reject, 4, "Instrument je na servisu.");

        // 18. student4 | 214ce | Approved
        var r18 = Request("student4", "214ce", 3);
        Store(r18, RentalTrigger.Approve, 2);

        // 19. student4 | StingRay | Canceled
        var r19 = Request("student4", "StingRay", 20);
        Store(r19, RentalTrigger.Approve, 19);
        Store(r19, RentalTrigger.Cancel, 18, "Instrument je oštećen pri pregledu.");

        // 20. student5 | Imperialstar | Completed, paid
        var r20 = Request("student5", "Imperialstar", 30);
        Store(r20, RentalTrigger.Approve, 29);
        Store(r20, RentalTrigger.Pickup, 28);
        Store(r20, RentalTrigger.Complete, 10);
        Pay(r20, 9);

        // 21. student5 | Minilogue | Pending
        Request("student5", "Minilogue", 1);

        // 22. student6 | Katana-100 MkII | Completed, paid
        var r22 = Request("student6", "Katana-100 MkII", 55);
        Store(r22, RentalTrigger.Approve, 54);
        Store(r22, RentalTrigger.Pickup, 53);
        Store(r22, RentalTrigger.Complete, 25);
        Pay(r22, 24);

        // 23. student6 | Rocker 15 | Active
        var r23 = Request("student6", "Rocker 15", 4);
        Store(r23, RentalTrigger.Approve, 3);
        Store(r23, RentalTrigger.Pickup, 2);

        context.Set<InstrumentRental>().AddRange(rentals);
        await context.SaveChangesAsync();
    }

    public static async Task SeedInstrumentViews(ENoteContext context, IClock clock)
    {
        if (await context.Set<InstrumentView>().AnyAsync())
        {
            return;
        }

        var studentUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "student");
        if (studentUser == null)
        {
            return;
        }

        var instruments = await context.Set<Instrument>().ToDictionaryAsync(i => i.Model, i => i.Id);
        var now = clock.UtcNow;

        var views = new List<InstrumentView>();

        if (instruments.TryGetValue("Minilogue", out var minilogueId))
        {
            var view = new InstrumentView(studentUser.Id, minilogueId, now.AddDays(-1));
            view.RecordView(now.AddDays(-1));
            view.RecordView(now.AddDays(-1));
            views.Add(view);
        }

        if (instruments.TryGetValue("Juno-DS", out var junoId))
        {
            var view = new InstrumentView(studentUser.Id, junoId, now.AddDays(-2));
            view.RecordView(now.AddDays(-2));
            views.Add(view);
        }

        if (instruments.TryGetValue("D-28", out var d28Id))
        {
            views.Add(new InstrumentView(studentUser.Id, d28Id, now.AddDays(-4)));
        }

        if (instruments.TryGetValue("Blues Junior IV", out var bjId))
        {
            views.Add(new InstrumentView(studentUser.Id, bjId, now.AddDays(-5)));
        }

        context.Set<InstrumentView>().AddRange(views);
        await context.SaveChangesAsync();
    }

    private static void ApplyStep(
        InstrumentRental rental,
        RentalTrigger trigger,
        int actorUserId,
        RentalActor actor,
        decimal monthlyFee,
        DateTime at,
        string? responseNote = null)
    {
        var context = new RentalTransitionContext
        {
            UserId = actorUserId,
            Actor = actor,
            HasInstrumentLockConflict = false,
            MonthlyFee = monthlyFee,
            ResponseNote = responseNote
        };

        var result = rental.Transition(trigger, context, at);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}

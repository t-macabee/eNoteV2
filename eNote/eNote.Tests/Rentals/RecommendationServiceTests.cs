using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Rentals;

public sealed class RecommendationServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ReturnsEmpty_WhenNoCandidates()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ExcludesInstrumentsWithActiveRentals()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-10), null);
        rental.Approve(50m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-5));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_CompletedRentalsDoNotExcludeInstrument()
    {

        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-30), null);
        rental.Approve(50m, null, Now.AddDays(-30), 1);
        rental.Pickup(Now.AddDays(-25));
        rental.Complete(Now.AddDays(-1), null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Instrument.Id == instrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_HigherScoredInstrumentRanksFirst()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrumentA = new Instrument("InstrA", "YAM", null, null, typeA.Id, store.Id);
        var instrumentB = new Instrument("InstrB", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentA, instrumentB);
        await context.SaveChangesAsync();

        var r1 = new InstrumentRental(instrumentA.Id, student.Id, store.Id, Now.AddDays(-60), null);
        r1.Approve(50m, null, Now.AddDays(-60), 1);
        r1.Pickup(Now.AddDays(-55));
        r1.Complete(Now.AddDays(-30), null);
        context.Set<InstrumentRental>().Add(r1);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        Assert.Equal(instrumentA.Id, result[0].Instrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_Popularity_CountsAllRentals()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrumentRecent = new Instrument("Recent", "YAM", null, null, typeA.Id, store.Id);
        var instrumentOld = new Instrument("Old", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentRecent, instrumentOld);
        await context.SaveChangesAsync();

        var rOld1 = CreateCompletedRental(instrumentOld.Id, student.Id + 2, store.Id, Now.AddMonths(-8));
        var rOld2 = CreateCompletedRental(instrumentOld.Id, student.Id + 3, store.Id, Now.AddMonths(-8));
        var rRecent = CreateCompletedRental(instrumentRecent.Id, student.Id + 1, store.Id, Now.AddMonths(-2));

        context.Set<InstrumentRental>().AddRange(rOld1, rOld2, rRecent);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        var recentRec = result.First(r => r.Instrument.Id == instrumentRecent.Id);
        var oldRec = result.First(r => r.Instrument.Id == instrumentOld.Id);

        Assert.True(oldRec.Score > recentRec.Score);
        Assert.Contains(oldRec.Reasons, r => r.Contains("Popularan"));
        Assert.Contains(recentRec.Reasons, r => r.Contains("Popularan"));
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_CollaborativeOrdering_RanksBySharedRentalFrequency()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var commonInstrument = new Instrument("Common", "YAM", null, null, typeA.Id, store.Id);
        var collabFrequent = new Instrument("CollabFrequent", "YAM", null, null, typeA.Id, store.Id);
        var collabLessFrequent = new Instrument("CollabLessFrequent", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(commonInstrument, collabFrequent, collabLessFrequent);
        await context.SaveChangesAsync();

        var rStudent = new InstrumentRental(commonInstrument.Id, student.Id, store.Id, Now.AddDays(-60), null);
        rStudent.Approve(50m, null, Now.AddDays(-60), 1);
        rStudent.Pickup(Now.AddDays(-55));
        rStudent.Complete(Now.AddDays(-30), null);

        var rSim1_1 = new InstrumentRental(commonInstrument.Id, 201, store.Id, Now.AddDays(-50), null);
        rSim1_1.Approve(50m, null, Now.AddDays(-50), 1);
        rSim1_1.Pickup(Now.AddDays(-49));
        rSim1_1.Complete(Now.AddDays(-20), null);

        var rSim1_2 = new InstrumentRental(collabFrequent.Id, 201, store.Id, Now.AddDays(-50), null);
        rSim1_2.Approve(50m, null, Now.AddDays(-50), 1);
        rSim1_2.Pickup(Now.AddDays(-49));
        rSim1_2.Complete(Now.AddDays(-20), null);

        var rSim2_1 = new InstrumentRental(commonInstrument.Id, 202, store.Id, Now.AddDays(-45), null);
        rSim2_1.Approve(50m, null, Now.AddDays(-45), 1);
        rSim2_1.Pickup(Now.AddDays(-44));
        rSim2_1.Complete(Now.AddDays(-15), null);

        var rSim2_2 = new InstrumentRental(collabFrequent.Id, 202, store.Id, Now.AddDays(-45), null);
        rSim2_2.Approve(50m, null, Now.AddDays(-45), 1);
        rSim2_2.Pickup(Now.AddDays(-44));
        rSim2_2.Complete(Now.AddDays(-15), null);

        var rSim2_3 = new InstrumentRental(collabLessFrequent.Id, 202, store.Id, Now.AddDays(-45), null);
        rSim2_3.Approve(50m, null, Now.AddDays(-45), 1);
        rSim2_3.Pickup(Now.AddDays(-44));
        rSim2_3.Complete(Now.AddDays(-15), null);

        context.Set<InstrumentRental>().AddRange(rStudent, rSim1_1, rSim1_2, rSim2_1, rSim2_2, rSim2_3);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Instrument.Id == collabFrequent.Id);
        Assert.Contains(result, r => r.Instrument.Id == collabLessFrequent.Id);
        Assert.Contains(result.First(r => r.Instrument.Id == collabFrequent.Id).Reasons, r => r.Contains("sličnim izborima"));
        Assert.Contains(result.First(r => r.Instrument.Id == collabLessFrequent.Id).Reasons, r => r.Contains("sličnim izborima"));
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_CandidatePoolTruncation_KeepsHigherSharedRentalCountInstrument()
    {
        // LoadCandidateInstrumentsAsync's pool is capped at CandidatePoolSize (80) regardless
        // of the requested count. This seeds exactly one candidate over that budget so the
        // deterministic collaborative order (by shared-rental count desc, then instrument id
        // asc) decides which one is dropped: 78 tied "filler" candidates plus a high-count and
        // a low-count boundary pair. The low-count instrument is created last so it has the
        // highest id among the count=1 tier, guaranteeing it is the one pushed past the cutoff.
        const int fillerCount = 78;

        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var commonInstrument = new Instrument("Common", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(commonInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(commonInstrument.Id, student.Id, store.Id, Now.AddDays(-90)));
        await context.SaveChangesAsync();

        var fillers = new List<Instrument>();
        for (var i = 0; i < fillerCount; i++)
        {
            fillers.Add(new Instrument($"Filler{i}", "YAM", null, null, typeB.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(fillers);
        await context.SaveChangesAsync();

        var highCountInstrument = new Instrument("HighCount", "YAM", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().Add(highCountInstrument);
        await context.SaveChangesAsync();

        var lowCountInstrument = new Instrument("LowCount", "YAM", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().Add(lowCountInstrument);
        await context.SaveChangesAsync();

        var rentals = new List<InstrumentRental>();
        var similarStudentId = 300;
        foreach (var filler in fillers)
        {
            rentals.Add(CreateCompletedRental(commonInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            rentals.Add(CreateCompletedRental(filler.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            similarStudentId++;
        }

        for (var i = 0; i < 2; i++)
        {
            rentals.Add(CreateCompletedRental(commonInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            rentals.Add(CreateCompletedRental(highCountInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            similarStudentId++;
        }

        rentals.Add(CreateCompletedRental(commonInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
        rentals.Add(CreateCompletedRental(lowCountInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));

        context.Set<InstrumentRental>().AddRange(rentals);
        context.Set<InstrumentView>().AddRange(
            new InstrumentView(student.AppUserId, highCountInstrument.Id, Now.AddDays(-5)),
            new InstrumentView(student.AppUserId, lowCountInstrument.Id, Now.AddDays(-5)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var resultIds = result.Select(r => r.Instrument.Id).ToHashSet();
        Assert.Contains(highCountInstrument.Id, resultIds);
        Assert.DoesNotContain(lowCountInstrument.Id, resultIds);
    }

    private static InstrumentRental CreateCompletedRental(int instrumentId, int studentProfileId, int storeId, DateTime requestedAt)
    {
        var rental = new InstrumentRental(instrumentId, studentProfileId, storeId, requestedAt, null);
        rental.Approve(50m, null, requestedAt, 1);
        rental.Pickup(requestedAt.AddDays(1));
        rental.Complete(requestedAt.AddDays(5), null);
        return rental;
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context)
    {
        var student = new Student(appUserId: 100, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static async Task<(InstrumentType TypeA, InstrumentType TypeB)> SeedTwoTypesAsync(ENoteContext context)
    {
        var typeA = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        var typeB = new InstrumentType { Type = "Piano", MonthlyFee = 80m };
        context.Set<InstrumentType>().AddRange(typeA, typeB);
        await context.SaveChangesAsync();
        return (typeA, typeB);
    }

    private static async Task<MusicStore> SeedStoreAsync(ENoteContext context)
    {
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        return store;
    }

    [Fact]
    public async Task RecordViewAsync_TreatsDuplicateViewViolation_AsSuccess()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentViewUserIdInstrumentIdUniqueIndex}\"");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)), student);

        await service.RecordInstrumentViewAsync(instrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_InstrumentRentedByOtherStudent_HasIsAvailableFalse()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var viewingStudent = await SeedStudentAsync(context);
        var otherStudent = new Student(appUserId: 101, Now.AddMonths(-1));
        otherStudent.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(otherStudent);

        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var activeRental = new InstrumentRental(instrument.Id, otherStudent.Id, instrument.MusicStoreId, Now.AddDays(-5), null);
        activeRental.Approve(50m, null, Now.AddDays(-5), 1);
        activeRental.Pickup(Now.AddDays(-4));
        context.Set<InstrumentRental>().Add(activeRental);
        await context.SaveChangesAsync();

        var service = CreateService(context, viewingStudent);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        var rec = Assert.Single(result, r => r.Instrument.Id == instrument.Id);
        Assert.False(rec.Instrument.IsAvailable);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_TieBreaksByAvailability_WhenScoresAreEqual()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var viewingStudent = await SeedStudentAsync(context);
        var otherStudent = new Student(appUserId: 101, Now.AddMonths(-1));
        otherStudent.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(otherStudent);

        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrumentAvailable = new Instrument("Available", "YAM", null, null, typeA.Id, store.Id);
        var instrumentRented = new Instrument("Rented", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentAvailable, instrumentRented);
        await context.SaveChangesAsync();

        var completedRental = new InstrumentRental(instrumentAvailable.Id, otherStudent.Id, store.Id, Now.AddDays(-5), null);
        completedRental.Approve(50m, null, Now.AddDays(-5), 1);
        completedRental.Pickup(Now.AddDays(-4));
        completedRental.Complete(Now.AddDays(-3), null);

        var activeRental = new InstrumentRental(instrumentRented.Id, otherStudent.Id, store.Id, Now.AddDays(-5), null);
        activeRental.Approve(50m, null, Now.AddDays(-5), 1);
        activeRental.Pickup(Now.AddDays(-4));
        context.Set<InstrumentRental>().AddRange(completedRental, activeRental);
        await context.SaveChangesAsync();

        var service = CreateService(context, viewingStudent);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.True(result.Count >= 2);
        var recAvailable = result.First(r => r.Instrument.Id == instrumentAvailable.Id);
        var recRented = result.First(r => r.Instrument.Id == instrumentRented.Id);

        Assert.True(recAvailable.Instrument.IsAvailable);
        Assert.False(recRented.Instrument.IsAvailable);
        var list = result.ToList();
        var indexOfAvailable = list.IndexOf(recAvailable);
        var indexOfRented = list.IndexOf(recRented);
        Assert.True(indexOfAvailable < indexOfRented);
    }

    private static RecommendationService CreateService(IAppDbContext context, Student student)
    {
        var currentUser = new StubCurrentActor(student: student, storeId: 1);
        return new(context, TestMapper.Create(), currentUser, currentUser, new FixedClock(Now), NullLogger<RecommendationService>.Instance);
    }
}

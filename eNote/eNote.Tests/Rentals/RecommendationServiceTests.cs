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
    public async Task GetRecommendedInstrumentsAsync_PopularityCutoff_ExcludesRentalsOlderThan6Months()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrumentRecent = new Instrument("Recent", "YAM", null, null, typeA.Id, store.Id);
        var instrumentOld = new Instrument("Old", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentRecent, instrumentOld);
        await context.SaveChangesAsync();

        var rRecent = new InstrumentRental(instrumentRecent.Id, student.Id + 1, store.Id, Now.AddMonths(-2), null);
        rRecent.Approve(50m, null, Now.AddMonths(-2), 1);
        rRecent.Pickup(Now.AddMonths(-2));
        rRecent.Complete(Now.AddMonths(-1), null);

        var rOld = new InstrumentRental(instrumentOld.Id, student.Id + 2, store.Id, Now.AddMonths(-8), null);
        rOld.Approve(50m, null, Now.AddMonths(-8), 1);
        rOld.Pickup(Now.AddMonths(-8));
        rOld.Complete(Now.AddMonths(-7), null);

        context.Set<InstrumentRental>().AddRange(rRecent, rOld);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        var recentRec = result.First(r => r.Instrument.Id == instrumentRecent.Id);
        var oldRec = result.First(r => r.Instrument.Id == instrumentOld.Id);

        Assert.True(recentRec.Score > oldRec.Score);
        Assert.Contains(recentRec.Reasons, r => r.Contains("Popularan"));
        Assert.DoesNotContain(oldRec.Reasons, r => r.Contains("Popularan"));
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

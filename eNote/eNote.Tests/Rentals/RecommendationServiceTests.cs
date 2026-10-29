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
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-10), pickedUpAt: Now.AddDays(-5));
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
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-30), pickedUpAt: Now.AddDays(-25), finishedAt: Now.AddDays(-1));
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
        RentalTestData.AdvanceTo(r1, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-60), pickedUpAt: Now.AddDays(-55), finishedAt: Now.AddDays(-30));
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
        RentalTestData.AdvanceTo(rStudent, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-60), pickedUpAt: Now.AddDays(-55), finishedAt: Now.AddDays(-30));

        var rSim1_1 = new InstrumentRental(commonInstrument.Id, 201, store.Id, Now.AddDays(-50), null);
        RentalTestData.AdvanceTo(rSim1_1, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-50), pickedUpAt: Now.AddDays(-49), finishedAt: Now.AddDays(-20));

        var rSim1_2 = new InstrumentRental(collabFrequent.Id, 201, store.Id, Now.AddDays(-50), null);
        RentalTestData.AdvanceTo(rSim1_2, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-50), pickedUpAt: Now.AddDays(-49), finishedAt: Now.AddDays(-20));

        var rSim2_1 = new InstrumentRental(commonInstrument.Id, 202, store.Id, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rSim2_1, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-44), finishedAt: Now.AddDays(-15));

        var rSim2_2 = new InstrumentRental(collabFrequent.Id, 202, store.Id, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rSim2_2, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-44), finishedAt: Now.AddDays(-15));

        var rSim2_3 = new InstrumentRental(collabLessFrequent.Id, 202, store.Id, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rSim2_3, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-44), finishedAt: Now.AddDays(-15));

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
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var resultIds = result.Select(r => r.Instrument.Id).ToHashSet();
        Assert.Contains(highCountInstrument.Id, resultIds);
        Assert.DoesNotContain(lowCountInstrument.Id, resultIds);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ViewedInstrument_IsCandidateOutsidePreferredTypesCollaborativeAndPopular()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var ownInstrument = new Instrument("Own", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(ownInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(ownInstrument.Id, student.Id, store.Id, Now.AddDays(-90)));

        var rentedByOthers = new List<Instrument>();
        for (var i = 0; i < 40; i++)
        {
            rentedByOthers.Add(new Instrument($"Other{i}", "ROL", null, null, typeB.Id, store.Id));
        }

        context.Set<Instrument>().AddRange(rentedByOthers);
        await context.SaveChangesAsync();

        var similarStudentId = 400;
        foreach (var instrument in rentedByOthers)
        {
            context.Set<InstrumentRental>().Add(CreateCompletedRental(instrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            similarStudentId++;
        }

        var viewedInstrument = new Instrument("Viewed", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().Add(viewedInstrument);
        await context.SaveChangesAsync();

        var view = new InstrumentView(student.AppUserId, viewedInstrument.Id, Now.AddDays(-5));
        for (var i = 0; i < 4; i++)
        {
            view.RecordView(Now.AddDays(-5 + i));
        }

        context.Set<InstrumentView>().Add(view);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result, r => r.Instrument.Id == viewedInstrument.Id);
        Assert.Contains(recommendation.Reasons, r => r.Contains("Pregledali ste"));
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ExcludesArchivedInstrument_EvenWhenPopular()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var archived = new Instrument("Archived", "YAM", null, null, typeA.Id, store.Id);
        var active = new Instrument("Active", "YAM", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(archived, active);
        await context.SaveChangesAsync();

        var rentals = new List<InstrumentRental>();
        for (var similarStudentId = 201; similarStudentId < 204; similarStudentId++)
        {
            rentals.Add(CreateCompletedRental(archived.Id, similarStudentId, store.Id, Now.AddDays(-40)));
        }

        context.Set<InstrumentRental>().AddRange(rentals);
        await context.SaveChangesAsync();

        archived.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.DoesNotContain(result, r => r.Instrument.Id == archived.Id);
        Assert.Contains(result, r => r.Instrument.Id == active.Id);
    }

    private static InstrumentRental CreateCompletedRental(int instrumentId, int studentProfileId, int storeId, DateTime requestedAt)
    {
        var rental = new InstrumentRental(instrumentId, studentProfileId, storeId, requestedAt, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Completed, requestedAt, approvedAt: requestedAt, pickedUpAt: requestedAt.AddDays(1), finishedAt: requestedAt.AddDays(5));
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
        RentalTestData.AdvanceTo(activeRental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4));
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
        RentalTestData.AdvanceTo(completedRental, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4), finishedAt: Now.AddDays(-3));

        var activeRental = new InstrumentRental(instrumentRented.Id, otherStudent.Id, store.Id, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(activeRental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4));
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

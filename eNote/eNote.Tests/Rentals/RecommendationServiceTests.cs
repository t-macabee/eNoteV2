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
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ExcludesInstrumentsWithActiveRentals()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
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
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-30), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-31), pickedUpAt: Now.AddDays(-31), finishedAt: Now.AddDays(-1));
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
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrumentA = new Instrument("InstrA", "YAM", null, null, typeA.Id, store.Id);
        var instrumentB = new Instrument("InstrB", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentA, instrumentB);
        await context.SaveChangesAsync();

        var r1 = new InstrumentRental(instrumentA.Id, student.Id, store.Id, Now.AddDays(-60), null);
        RentalTestData.AdvanceTo(r1, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-60), pickedUpAt: Now.AddDays(-60), finishedAt: Now.AddDays(-30));
        context.Set<InstrumentRental>().Add(r1);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        Assert.Equal(instrumentA.Id, result[0].Instrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ScoresManufacturerAndTypeMatches()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrumentA = new Instrument("InstrA", "YAM", null, null, typeA.Id, store.Id);
        var instrumentB = new Instrument("InstrB", "ROL", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentA, instrumentB);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(instrumentA.Id, student.Id, store.Id, Now.AddDays(-40)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(instrumentA.Id, result[0].Instrument.Id);
        Assert.Equal(0.645, result[0].Score);
        Assert.Equal(instrumentB.Id, result[1].Instrument.Id);
        Assert.Equal(0.465, result[1].Score);
        Assert.Equal(
            [
                "Na osnovu vaše historije najma (Guitar).",
                "Sličan vašim prethodnim izborima proizvođača ili vrste.",
                "Popularan među studentima."
            ],
            result[0].Reasons);
        Assert.Equal(
            [
                "Na osnovu vaše historije najma (Guitar).",
                "Sličan vašim prethodnim izborima proizvođača ili vrste."
            ],
            result[1].Reasons);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(7, 7)]
    [InlineData(25, 20)]
    public async Task GetRecommendedInstrumentsAsync_ClampsCount(int count, int expected)
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instruments = new List<Instrument>();
        for (var i = 0; i < 22; i++)
        {
            instruments.Add(new Instrument($"I{i}", "YAM", null, null, typeA.Id, store.Id));
        }

        context.Set<Instrument>().AddRange(instruments);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(count);

        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ReturnsFiveByDefault()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instruments = new List<Instrument>();
        for (var i = 0; i < 8; i++)
        {
            instruments.Add(new Instrument($"D{i}", "YAM", null, null, typeA.Id, store.Id));
        }

        context.Set<Instrument>().AddRange(instruments);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_PoolGrowsWithRequestedCount()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rented = new Instrument("Rented", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(rented);
        await context.SaveChangesAsync();

        var rolInstruments = new List<Instrument>();
        for (var i = 0; i < 40; i++)
        {
            rolInstruments.Add(new Instrument($"R{i}", "ROL", null, null, typeA.Id, store.Id));
        }

        context.Set<Instrument>().AddRange(rolInstruments);
        await context.SaveChangesAsync();

        var lateYamaha = new List<Instrument>();
        for (var i = 0; i < 10; i++)
        {
            lateYamaha.Add(new Instrument($"Y{i}", "YAM", null, null, typeA.Id, store.Id));
        }

        context.Set<Instrument>().AddRange(lateYamaha);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(rented.Id, student.Id, store.Id, Now.AddDays(-40)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(20);

        Assert.Equal(20, result.Count);
        var resultIds = result.Select(r => r.Instrument.Id).ToHashSet();
        Assert.All(lateYamaha, instrument => Assert.Contains(instrument.Id, resultIds));
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_Popularity_CountsAllRentals()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
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
    public async Task GetRecommendedInstrumentsAsync_CollaborativeInstruments_GetTheCollaborativeReason()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var commonInstrument = new Instrument("Common", "YAM", null, null, typeA.Id, store.Id);
        var collabFrequent = new Instrument("CollabFrequent", "YAM", null, null, typeA.Id, store.Id);
        var collabLessFrequent = new Instrument("CollabLessFrequent", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(commonInstrument, collabFrequent, collabLessFrequent);
        await context.SaveChangesAsync();

        var rStudent = new InstrumentRental(commonInstrument.Id, student.Id, store.Id, Now.AddDays(-60), null);
        RentalTestData.AdvanceTo(rStudent, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-60), pickedUpAt: Now.AddDays(-60), finishedAt: Now.AddDays(-30));

        var rSim1_1 = new InstrumentRental(commonInstrument.Id, 201, store.Id, Now.AddDays(-50), null);
        RentalTestData.AdvanceTo(rSim1_1, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-50), pickedUpAt: Now.AddDays(-50), finishedAt: Now.AddDays(-20));

        var rSim1_2 = new InstrumentRental(collabFrequent.Id, 201, store.Id, Now.AddDays(-50), null);
        RentalTestData.AdvanceTo(rSim1_2, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-50), pickedUpAt: Now.AddDays(-50), finishedAt: Now.AddDays(-20));

        var rSim2_1 = new InstrumentRental(commonInstrument.Id, 202, store.Id, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rSim2_1, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-45), finishedAt: Now.AddDays(-15));

        var rSim2_2 = new InstrumentRental(collabFrequent.Id, 202, store.Id, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rSim2_2, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-45), finishedAt: Now.AddDays(-15));

        var rSim2_3 = new InstrumentRental(collabLessFrequent.Id, 202, store.Id, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rSim2_3, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-45), finishedAt: Now.AddDays(-15));

        context.Set<InstrumentRental>().AddRange(rStudent, rSim1_1, rSim1_2, rSim2_1, rSim2_2, rSim2_3);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Instrument.Id == collabFrequent.Id);
        Assert.Contains(result, r => r.Instrument.Id == collabLessFrequent.Id);
        Assert.Contains(result.First(r => r.Instrument.Id == collabFrequent.Id).Reasons, r => r.Contains("sličnim izborima"));
        Assert.Contains(result.First(r => r.Instrument.Id == collabLessFrequent.Id).Reasons, r => r.Contains("sličnim izborima"));
        var commonRecommendation = Assert.Single(result, r => r.Instrument.Id == commonInstrument.Id);
        Assert.DoesNotContain(commonRecommendation.Reasons, r => r.Contains("sličnim izborima"));
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_CandidatePoolTruncation_KeepsTheMoreFrequentCollaborativeInstrument()
    {
        const int fillerCount = 79;

        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
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
            fillers.Add(new Instrument($"Filler{i}", "ROL", null, null, typeB.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(fillers);
        await context.SaveChangesAsync();

        var lowCountInstrument = new Instrument("LowCount", "YAM", null, null, typeB.Id, store.Id);
        var highCountInstrument = new Instrument("HighCount", "YAM", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(lowCountInstrument, highCountInstrument);
        await context.SaveChangesAsync();

        var rentals = new List<InstrumentRental>();
        var similarStudentId = 300;
        foreach (var filler in fillers)
        {
            rentals.Add(CreateCompletedRental(commonInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            rentals.Add(CreateCompletedRental(filler.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            similarStudentId++;
        }

        rentals.Add(CreateCompletedRental(commonInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
        rentals.Add(CreateCompletedRental(lowCountInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
        similarStudentId++;

        for (var i = 0; i < 2; i++)
        {
            rentals.Add(CreateCompletedRental(commonInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            rentals.Add(CreateCompletedRental(highCountInstrument.Id, similarStudentId, store.Id, Now.AddDays(-70)));
            similarStudentId++;
        }

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
        var student = await RentalTestData.SeedStudentAsync(context, Now);
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
        var student = await RentalTestData.SeedStudentAsync(context, Now);
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

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ReturnsMonthlyFeeOfInstrumentType()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var store = await SeedStoreAsync(context);
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 45m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();
        var instrument = new Instrument("Stratocaster", "Fender", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result, r => r.Instrument.Id == instrument.Id);
        Assert.Equal(45m, recommendation.Instrument.MonthlyFee);
    }

    private static InstrumentRental CreateCompletedRental(int instrumentId, int studentProfileId, int storeId, DateTime requestedAt)
    {
        var rental = new InstrumentRental(instrumentId, studentProfileId, storeId, requestedAt, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Completed, requestedAt, approvedAt: requestedAt.AddDays(-25), pickedUpAt: requestedAt.AddDays(-25), finishedAt: requestedAt.AddDays(5));
        return rental;
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
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentViewUserIdInstrumentIdUniqueIndex}\"");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)), student);

        await service.RecordInstrumentViewAsync(instrument.Id);
    }

    [Fact]
    public async Task RecordViewAsync_CalledTwice_KeepsOneRow()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var service = CreateService(context, student);

        await service.RecordInstrumentViewAsync(instrument.Id);
        await service.RecordInstrumentViewAsync(instrument.Id);

        var view = Assert.Single(await context.Set<InstrumentView>().ToListAsync());
        Assert.Equal(student.AppUserId, view.UserId);
        Assert.Equal(2, view.ViewCount);
        Assert.Equal(Now, view.LastViewedAt);
    }

    [Fact]
    public async Task RecordViewAsync_OnArchivedInstrument_ThrowsAndWritesNoRow()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RecordInstrumentViewAsync(instrument.Id));

        Assert.Empty(await context.Set<InstrumentView>().ToListAsync());
    }

    [Fact]
    public async Task RecordViewAsync_OnUnknownInstrument_ThrowsAndWritesNoRow()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RecordInstrumentViewAsync(int.MaxValue));

        Assert.Empty(await context.Set<InstrumentView>().ToListAsync());
    }

    [Fact]
    public async Task RecordViewAsync_PropagatesADbUpdateExceptionForAnotherIndex()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentTypeTypeUniqueIndex}\"");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)), student);

        await Assert.ThrowsAsync<DbUpdateException>(() => service.RecordInstrumentViewAsync(instrument.Id));
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Active)]
    public async Task GetRecommendedInstrumentsAsync_InstrumentHeldByAnotherStudent_IsNotRecommended(InstrumentRentalStatus status)
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var heldInstrument = new Instrument("Held", "YAM", null, null, typeA.Id, store.Id);
        var freeInstrument = new Instrument("Free", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(heldInstrument, freeInstrument);
        await context.SaveChangesAsync();

        var rental = new InstrumentRental(heldInstrument.Id, 500, store.Id, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(rental, status, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.DoesNotContain(result, r => r.Instrument.Id == heldInstrument.Id);
        var recommendation = Assert.Single(result, r => r.Instrument.Id == freeInstrument.Id);
        Assert.True(recommendation.Instrument.IsAvailable);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_InstrumentWithOwnApprovedRental_IsNotRecommended()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var instrument = new Instrument("Held", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        var rental = new InstrumentRental(instrument.Id, student.Id, store.Id, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now, approvedAt: Now.AddDays(-5));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_PendingRequest_ExcludesTheStudentsOwnAndKeepsAnotherStudents()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var ownPendingInstrument = new Instrument("OwnPending", "YAM", null, null, typeA.Id, store.Id);
        var otherPendingInstrument = new Instrument("OtherPending", "ROL", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(ownPendingInstrument, otherPendingInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().AddRange(
            new InstrumentRental(ownPendingInstrument.Id, student.Id, store.Id, Now.AddDays(-1), null),
            new InstrumentRental(otherPendingInstrument.Id, 500, store.Id, Now.AddDays(-1), null));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.DoesNotContain(result, r => r.Instrument.Id == ownPendingInstrument.Id);
        Assert.Contains(result, r => r.Instrument.Id == otherPendingInstrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ArchivedViews_DoNotTakeViewSlots()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var archivedInstruments = new List<Instrument>();
        for (var i = 0; i < 20; i++)
        {
            archivedInstruments.Add(new Instrument($"Archived{i}", "YAM", null, null, typeA.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(archivedInstruments);
        await context.SaveChangesAsync();

        context.Set<InstrumentView>().AddRange(archivedInstruments.Select(x => CreateView(student.AppUserId, x.Id, 5)));
        await context.SaveChangesAsync();

        foreach (var instrument in archivedInstruments)
        {
            instrument.SoftDelete();
        }
        await context.SaveChangesAsync();

        var viewedInstrument = new Instrument("Viewed", "ROL", null, null, typeB.Id, store.Id);
        var rentedInstruments = new List<Instrument>();
        for (var i = 0; i < 6; i++)
        {
            rentedInstruments.Add(new Instrument($"Rented{i}", "KOR", null, null, typeB.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(rentedInstruments);
        context.Set<Instrument>().Add(viewedInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentView>().Add(CreateView(student.AppUserId, viewedInstrument.Id, 1));

        var rentals = new List<InstrumentRental>();
        for (var i = 0; i < rentedInstruments.Count; i++)
        {
            rentals.Add(CreateCompletedRental(rentedInstruments[i].Id, 600 + i, store.Id, Now.AddDays(-40)));
        }
        context.Set<InstrumentRental>().AddRange(rentals);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Contains(result, r => r.Instrument.Id == viewedInstrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ArchivedViews_DoNotLowerTheViewSignal()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var archivedInstrument = new Instrument("Archived", "YAM", null, null, typeA.Id, store.Id);
        var activeInstrument = new Instrument("Active", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(archivedInstrument, activeInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentView>().AddRange(
            CreateView(student.AppUserId, archivedInstrument.Id, 10),
            CreateView(student.AppUserId, activeInstrument.Id, 3));
        await context.SaveChangesAsync();

        archivedInstrument.SoftDelete();
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result);
        Assert.Equal(activeInstrument.Id, recommendation.Instrument.Id);
        Assert.Equal(0.3, recommendation.Score);
        Assert.Equal(["Pregledali ste ovaj instrument."], recommendation.Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ArchivedRentals_DoNotLowerThePopularitySignal()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var archivedInstrument = new Instrument("Archived", "YAM", null, null, typeA.Id, store.Id);
        var activeInstrument = new Instrument("Active", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(archivedInstrument, activeInstrument);
        await context.SaveChangesAsync();

        var rentals = new List<InstrumentRental>();
        rentals.AddRange(CreateCompletedRentals(archivedInstrument.Id, store.Id, 500, 10, Now.AddDays(-40)));
        rentals.AddRange(CreateCompletedRentals(activeInstrument.Id, store.Id, 600, 4, Now.AddDays(-40)));
        context.Set<InstrumentRental>().AddRange(rentals);
        await context.SaveChangesAsync();

        archivedInstrument.SoftDelete();
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result);
        Assert.Equal(activeInstrument.Id, recommendation.Instrument.Id);
        Assert.Equal(0.1, recommendation.Score);
        Assert.Equal(["Popularan među studentima."], recommendation.Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_HeldInstrument_DoesNotShortenTheList()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var typeAInstruments = new List<Instrument>();
        for (var i = 0; i < 5; i++)
        {
            typeAInstruments.Add(new Instrument($"Guitar{i}", "YAM", null, null, typeA.Id, store.Id));
        }
        var typeBInstruments = new List<Instrument>();
        for (var i = 0; i < 5; i++)
        {
            typeBInstruments.Add(new Instrument($"Piano{i}", "ROL", null, null, typeB.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(typeAInstruments);
        context.Set<Instrument>().AddRange(typeBInstruments);
        await context.SaveChangesAsync();

        var heldInstrument = typeAInstruments[0];
        var rental = new InstrumentRental(heldInstrument.Id, student.Id, store.Id, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(5, result.Count);
        Assert.DoesNotContain(result, r => r.Instrument.Id == heldInstrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_HistoryReason_ForASecondType()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var guitarInstruments = new List<Instrument>();
        for (var i = 0; i < 3; i++)
        {
            guitarInstruments.Add(new Instrument($"Guitar{i}", "AAA", null, null, typeA.Id, store.Id));
        }
        var pianoInstruments = new List<Instrument>();
        for (var i = 0; i < 2; i++)
        {
            pianoInstruments.Add(new Instrument($"Piano{i}", "BBB", null, null, typeB.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(guitarInstruments);
        context.Set<Instrument>().AddRange(pianoInstruments);
        await context.SaveChangesAsync();

        var rentals = new List<InstrumentRental>();
        foreach (var instrument in guitarInstruments.Concat(pianoInstruments))
        {
            rentals.Add(CreateCompletedRental(instrument.Id, student.Id, store.Id, Now.AddDays(-40)));
        }
        context.Set<InstrumentRental>().AddRange(rentals);
        await context.SaveChangesAsync();

        var targetInstrument = new Instrument("NewPiano", "TGT", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().Add(targetInstrument);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(10);

        var recommendation = Assert.Single(result, r => r.Instrument.Id == targetInstrument.Id);
        Assert.Contains(recommendation.Reasons, r => r == "Na osnovu vaše historije najma (Piano).");
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_SecondSignalNeverLowersTheScore()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var typeC = new InstrumentType { Type = "Violin", MonthlyFee = 60m };
        context.Set<InstrumentType>().Add(typeC);
        await context.SaveChangesAsync();
        var store = await SeedStoreAsync(context);

        var guitarInstruments = new List<Instrument>();
        for (var i = 0; i < 5; i++)
        {
            guitarInstruments.Add(new Instrument($"Guitar{i}", "AAA", null, null, typeA.Id, store.Id));
        }
        var studentPianoInstrument = new Instrument("StudentPiano", "AAA", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(guitarInstruments);
        context.Set<Instrument>().Add(studentPianoInstrument);
        await context.SaveChangesAsync();

        var studentRentals = new List<InstrumentRental>();
        foreach (var instrument in guitarInstruments.Append(studentPianoInstrument))
        {
            studentRentals.Add(CreateCompletedRental(instrument.Id, student.Id, store.Id, Now.AddDays(-40)));
        }
        context.Set<InstrumentRental>().AddRange(studentRentals);
        await context.SaveChangesAsync();

        var collaborativeTypeB = new Instrument("X", "XXX", null, null, typeB.Id, store.Id);
        var collaborativeTypeC = new Instrument("Y", "YYY", null, null, typeC.Id, store.Id);
        context.Set<Instrument>().AddRange(collaborativeTypeB, collaborativeTypeC);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().AddRange(
            CreateCompletedRental(guitarInstruments[0].Id, 500, store.Id, Now.AddDays(-30)),
            CreateCompletedRental(collaborativeTypeB.Id, 500, store.Id, Now.AddDays(-30)),
            CreateCompletedRental(collaborativeTypeC.Id, 500, store.Id, Now.AddDays(-30)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(20);

        var typeBRecommendation = Assert.Single(result, r => r.Instrument.Id == collaborativeTypeB.Id);
        var typeCRecommendation = Assert.Single(result, r => r.Instrument.Id == collaborativeTypeC.Id);
        Assert.True(typeBRecommendation.Score > typeCRecommendation.Score);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_Similarity_MatchesAnyEarlierManufacturer()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rentedInstruments = new List<Instrument>
        {
            new("FenderOne", "Fender", null, null, typeA.Id, store.Id),
            new("FenderTwo", "Fender", null, null, typeA.Id, store.Id),
            new("GibsonRented", "Gibson", null, null, typeA.Id, store.Id)
        };
        var gibsonTarget = new Instrument("GibsonTarget", "Gibson", null, null, typeB.Id, store.Id);
        var fenderTarget = new Instrument("FenderTarget", "Fender", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(rentedInstruments);
        context.Set<Instrument>().AddRange(gibsonTarget, fenderTarget);
        await context.SaveChangesAsync();

        var rentals = new List<InstrumentRental>();
        foreach (var instrument in rentedInstruments)
        {
            rentals.Add(CreateCompletedRental(instrument.Id, student.Id, store.Id, Now.AddDays(-40)));
        }
        context.Set<InstrumentRental>().AddRange(rentals);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(10);

        foreach (var target in new[] { gibsonTarget, fenderTarget })
        {
            var recommendation = Assert.Single(result, r => r.Instrument.Id == target.Id);
            Assert.Contains(recommendation.Reasons, r => r == "Sličan vašim prethodnim izborima proizvođača ili vrste.");
        }
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ComputesTheRentalSignal()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var commonInstrument = new Instrument("Common", "YAM", null, null, typeA.Id, store.Id);
        var typeAInstrument = new Instrument("X", "ROL", null, null, typeA.Id, store.Id);
        var typeBInstrument = new Instrument("Y", "KOR", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(commonInstrument, typeAInstrument, typeBInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().AddRange(
            CreateCompletedRental(commonInstrument.Id, student.Id, store.Id, Now.AddDays(-40)),
            CreateCompletedRental(commonInstrument.Id, 500, store.Id, Now.AddDays(-35)),
            CreateCompletedRental(typeAInstrument.Id, 500, store.Id, Now.AddDays(-35)),
            CreateCompletedRental(typeBInstrument.Id, 500, store.Id, Now.AddDays(-35)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(typeAInstrument.Id, result[0].Instrument.Id);
        Assert.Equal(0.675, result[0].Score);
        Assert.Equal(
            [
                "Na osnovu vaše historije najma (Guitar).",
                "Studenti sa sličnim izborima najma biraju ovaj instrument.",
                "Sličan vašim prethodnim izborima proizvođača ili vrste.",
                "Popularan među studentima."
            ],
            result[0].Reasons);
        Assert.Equal(commonInstrument.Id, result[1].Instrument.Id);
        Assert.Equal(0.645, result[1].Score);
        Assert.Equal(
            [
                "Na osnovu vaše historije najma (Guitar).",
                "Sličan vašim prethodnim izborima proizvođača ili vrste.",
                "Popularan među studentima."
            ],
            result[1].Reasons);
        Assert.Equal(typeBInstrument.Id, result[2].Instrument.Id);
        Assert.Equal(0.21, result[2].Score);
        Assert.Equal(
            [
                "Studenti sa sličnim izborima najma biraju ovaj instrument.",
                "Popularan među studentima."
            ],
            result[2].Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ComputesTheViewSignal()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var mostViewed = new Instrument("A", "AAA", null, null, typeA.Id, store.Id);
        var twiceViewed = new Instrument("B", "BBB", null, null, typeA.Id, store.Id);
        var onceViewed = new Instrument("C", "CCC", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(mostViewed, twiceViewed, onceViewed);
        await context.SaveChangesAsync();

        context.Set<InstrumentView>().AddRange(
            CreateView(student.AppUserId, mostViewed.Id, 4),
            CreateView(student.AppUserId, twiceViewed.Id, 2),
            CreateView(student.AppUserId, onceViewed.Id, 1));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(mostViewed.Id, result[0].Instrument.Id);
        Assert.Equal(0.3, result[0].Score);
        Assert.Equal(["Pregledali ste ovaj instrument."], result[0].Reasons);
        Assert.Equal(twiceViewed.Id, result[1].Instrument.Id);
        Assert.Equal(0.15, result[1].Score);
        Assert.Equal(["Pregledali ste ovaj instrument."], result[1].Reasons);
        Assert.Equal(onceViewed.Id, result[2].Instrument.Id);
        Assert.Equal(0.075, result[2].Score);
        Assert.Equal(["Preporučeno na osnovu dostupnosti i ukupnog interesovanja."], result[2].Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_IgnoresViewsOfAnotherUser()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        context.Set<InstrumentView>().Add(CreateView(student.AppUserId + 1, instrument.Id, 5));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result);
        Assert.Equal(0, recommendation.Score);
        Assert.Equal(["Preporučeno na osnovu dostupnosti i ukupnog interesovanja."], recommendation.Reasons);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Rejected)]
    [InlineData(InstrumentRentalStatus.Canceled)]
    public async Task GetRecommendedInstrumentsAsync_RejectedOrCanceledRental_GivesNoHistory(InstrumentRentalStatus status)
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rentedInstrument = new Instrument("Rented", "YAM", null, null, typeA.Id, store.Id);
        var targetInstrument = new Instrument("Target", "ROL", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(rentedInstrument, targetInstrument);
        await context.SaveChangesAsync();

        var rental = new InstrumentRental(rentedInstrument.Id, student.Id, store.Id, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(rental, status, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result, r => r.Instrument.Id == targetInstrument.Id);
        Assert.Equal(0, recommendation.Score);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_EqualScores_ComeInIdOrder()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var secondInstrument = new Instrument("Second", "Yamaha", null, null, instrument.InstrumentTypeId, instrument.MusicStoreId);
        context.Set<Instrument>().Add(secondInstrument);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(instrument.Id, result[0].Instrument.Id);
        Assert.Equal(secondInstrument.Id, result[1].Instrument.Id);
        Assert.Equal(result[0].Score, result[1].Score);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_OwnHistory_ScalesWithTheMostRentedTypeAndKeepsTheReasonAtExactlyHalf()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var firstGuitar = new Instrument("FirstGuitar", "AAA", null, null, typeA.Id, store.Id);
        var secondGuitar = new Instrument("SecondGuitar", "AAA", null, null, typeA.Id, store.Id);
        var rentedPiano = new Instrument("RentedPiano", "BBB", null, null, typeB.Id, store.Id);
        var targetInstrument = new Instrument("Target", "TGT", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(firstGuitar, secondGuitar, rentedPiano, targetInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().AddRange(
            CreateCompletedRental(firstGuitar.Id, student.Id, store.Id, Now.AddDays(-40)),
            CreateCompletedRental(secondGuitar.Id, student.Id, store.Id, Now.AddDays(-40)),
            CreateCompletedRental(rentedPiano.Id, student.Id, store.Id, Now.AddDays(-40)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result, r => r.Instrument.Id == targetInstrument.Id);
        Assert.Equal(0.345, recommendation.Score);
        Assert.Equal(
            [
                "Na osnovu vaše historije najma (Piano).",
                "Sličan vašim prethodnim izborima proizvođača ili vrste."
            ],
            recommendation.Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_Similarity_IgnoresManufacturerCase()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rentedInstrument = new Instrument("Rented", "Fender", null, null, typeA.Id, store.Id);
        var targetInstrument = new Instrument("Target", "FENDER", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(rentedInstrument, targetInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(rentedInstrument.Id, student.Id, store.Id, Now.AddDays(-40)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        var recommendation = Assert.Single(result, r => r.Instrument.Id == targetInstrument.Id);
        Assert.Equal(0.2, recommendation.Score);
        Assert.Equal(["Sličan vašim prethodnim izborima proizvođača ili vrste."], recommendation.Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_EveryViewedInstrument_IsACandidate()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var unviewedInstruments = new List<Instrument>();
        for (var i = 0; i < 6; i++)
        {
            unviewedInstruments.Add(new Instrument($"Unviewed{i}", "AAA", null, null, typeA.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(unviewedInstruments);
        await context.SaveChangesAsync();

        var mostViewed = new Instrument("MostViewed", "BBB", null, null, typeA.Id, store.Id);
        var lessViewed = new Instrument("LessViewed", "CCC", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().AddRange(mostViewed, lessViewed);
        await context.SaveChangesAsync();

        context.Set<InstrumentView>().AddRange(
            CreateView(student.AppUserId, mostViewed.Id, 3),
            CreateView(student.AppUserId, lessViewed.Id, 1));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(mostViewed.Id, result[0].Instrument.Id);
        Assert.Equal(0.3, result[0].Score);
        Assert.Equal(lessViewed.Id, result[1].Instrument.Id);
        Assert.Equal(0.1, result[1].Score);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_PopularInstrumentWithAHighId_IsACandidate()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, _) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var neverRented = new List<Instrument>();
        for (var i = 0; i < 6; i++)
        {
            neverRented.Add(new Instrument($"NeverRented{i}", "AAA", null, null, typeA.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(neverRented);
        await context.SaveChangesAsync();

        var popularInstrument = new Instrument("Popular", "BBB", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(popularInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(popularInstrument.Id, 500, store.Id, Now.AddDays(-40)));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(popularInstrument.Id, result[0].Instrument.Id);
        Assert.Equal(0.1, result[0].Score);
        Assert.Equal(["Popularan među studentima."], result[0].Reasons);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_PreferredTypeLimit_LeavesRoomForOtherSources()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rentedInstrument = new Instrument("Rented", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(rentedInstrument);
        await context.SaveChangesAsync();

        var preferredInstruments = new List<Instrument>();
        for (var i = 0; i < 85; i++)
        {
            preferredInstruments.Add(new Instrument($"Preferred{i}", "ROL", null, null, typeA.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(preferredInstruments);
        await context.SaveChangesAsync();

        var targetInstrument = new Instrument("Target", "KOR", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().Add(targetInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().AddRange(
            CreateCompletedRental(rentedInstrument.Id, student.Id, store.Id, Now.AddDays(-40)),
            CreateCompletedRental(rentedInstrument.Id, 500, store.Id, Now.AddDays(-35)),
            CreateCompletedRental(targetInstrument.Id, 500, store.Id, Now.AddDays(-35)));
        context.Set<InstrumentView>().Add(CreateView(student.AppUserId, targetInstrument.Id, 1));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(rentedInstrument.Id, result[0].Instrument.Id);
        Assert.Equal(0.645, result[0].Score);
        Assert.Equal(targetInstrument.Id, result[1].Instrument.Id);
        Assert.Equal(0.51, result[1].Score);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_FullPool_KeepsViewedInstrumentsBeforeCollaborativeOnes()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rentedInstrument = new Instrument("Rented", "YAM", null, null, typeA.Id, store.Id);
        context.Set<Instrument>().Add(rentedInstrument);
        await context.SaveChangesAsync();

        var preferredInstruments = new List<Instrument>();
        for (var i = 0; i < 45; i++)
        {
            preferredInstruments.Add(new Instrument($"Preferred{i}", "ROL", null, null, typeA.Id, store.Id));
        }
        var collaborativeInstruments = new List<Instrument>();
        for (var i = 0; i < 45; i++)
        {
            collaborativeInstruments.Add(new Instrument($"Collaborative{i}", "KOR", null, null, typeB.Id, store.Id));
        }
        context.Set<Instrument>().AddRange(preferredInstruments);
        context.Set<Instrument>().AddRange(collaborativeInstruments);
        await context.SaveChangesAsync();

        var viewedInstrument = new Instrument("Viewed", "YAM", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().Add(viewedInstrument);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().AddRange(
            CreateCompletedRental(rentedInstrument.Id, student.Id, store.Id, Now.AddDays(-40)),
            CreateCompletedRental(rentedInstrument.Id, 500, store.Id, Now.AddDays(-35)));
        context.Set<InstrumentRental>().AddRange(collaborativeInstruments.Select(x => CreateCompletedRental(x.Id, 500, store.Id, Now.AddDays(-35))));
        context.Set<InstrumentView>().Add(CreateView(student.AppUserId, viewedInstrument.Id, 1));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(rentedInstrument.Id, result[0].Instrument.Id);
        Assert.Equal(0.645, result[0].Score);
        Assert.Equal(viewedInstrument.Id, result[1].Instrument.Id);
        Assert.Equal(0.5, result[1].Score);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ViewingAnInstrumentOfARentedType_NeverLowersItsScore()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await RentalTestData.SeedStudentAsync(context, Now);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        var rentedInstrument = new Instrument("Rented", "YAM", null, null, typeA.Id, store.Id);
        var viewedOnce = new Instrument("ViewedOnce", "ROL", null, null, typeA.Id, store.Id);
        var neverViewed = new Instrument("NeverViewed", "ROL", null, null, typeA.Id, store.Id);
        var mostViewed = new Instrument("MostViewed", "KOR", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(rentedInstrument, viewedOnce, neverViewed, mostViewed);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(CreateCompletedRental(rentedInstrument.Id, student.Id, store.Id, Now.AddDays(-40)));
        context.Set<InstrumentView>().AddRange(
            CreateView(student.AppUserId, mostViewed.Id, 4),
            CreateView(student.AppUserId, viewedOnce.Id, 1));
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(0.465, result.Single(r => r.Instrument.Id == viewedOnce.Id).Score);
        Assert.Equal(0.465, result.Single(r => r.Instrument.Id == neverViewed.Id).Score);
    }

    private static List<InstrumentRental> CreateCompletedRentals(int instrumentId, int storeId, int firstStudentProfileId, int count, DateTime requestedAt)
    {
        var rentals = new List<InstrumentRental>();
        for (var i = 0; i < count; i++)
        {
            rentals.Add(CreateCompletedRental(instrumentId, firstStudentProfileId + i, storeId, requestedAt));
        }
        return rentals;
    }

    private static InstrumentView CreateView(int userId, int instrumentId, int viewCount)
    {
        var view = new InstrumentView(userId, instrumentId, Now.AddDays(-1));
        for (var i = 1; i < viewCount; i++)
        {
            view.RecordView(Now.AddDays(-1));
        }
        return view;
    }

    private static RecommendationService CreateService(IAppDbContext context, Student student)
    {
        var currentUser = new StubCurrentActor(student: student, storeId: 1);
        return new(context, TestMapper.Create(), currentUser, currentUser, new FixedClock(Now), NullLogger<RecommendationService>.Instance);
    }
}

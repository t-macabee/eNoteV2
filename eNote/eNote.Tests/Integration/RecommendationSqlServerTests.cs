using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class RecommendationSqlServerTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_OnSqlServer_TranslatesTheNestedQueries()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var studentUser = new AppUser { UserName = "student", Email = "student@example.com", IsActive = true, FirstName = "Test", LastName = "User" };
        var otherUser = new AppUser { UserName = "other", Email = "other@example.com", IsActive = true, FirstName = "Test", LastName = "User" };
        context.Set<AppUser>().AddRange(studentUser, otherUser);
        await context.SaveChangesAsync();

        var student = new Student(studentUser.Id, Now.AddMonths(-1));
        var otherStudent = new Student(otherUser.Id, Now.AddMonths(-1));
        context.Set<Student>().AddRange(student, otherStudent);
        await context.SaveChangesAsync();

        var store = new MusicStore("Music Shop", "09-17");
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<MusicStore>().Add(store);
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var instrumentX = new Instrument("X", TestManufacturers.Id("XM"), null, null, type.Id, store.Id);
        var instrumentY = new Instrument("Y", TestManufacturers.Id("YM"), null, null, type.Id, store.Id);
        var instrumentZ = new Instrument("Z", TestManufacturers.Id("ZM"), null, null, type.Id, store.Id);
        var instrumentW = new Instrument("W", TestManufacturers.Id("WM"), null, null, type.Id, store.Id);
        var instrumentV = new Instrument("V", TestManufacturers.Id("VM"), null, null, type.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentX, instrumentY, instrumentZ, instrumentW, instrumentV);
        await context.SaveChangesAsync();

        instrumentV.SoftDelete();
        await context.SaveChangesAsync();

        var activeRental = new InstrumentRental(instrumentZ.Id, otherStudent.Id, store.Id, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(activeRental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4));

        context.Set<InstrumentRental>().AddRange(
            RentalTestData.CreateCompletedRental(instrumentX, student.Id, Now),
            new InstrumentRental(instrumentW.Id, student.Id, store.Id, Now.AddDays(-5), null),
            RentalTestData.CreateCompletedRental(instrumentX, otherStudent.Id, Now),
            RentalTestData.CreateCompletedRental(instrumentY, otherStudent.Id, Now),
            activeRental);
        await context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: student);
        var service = new RecommendationService(context, TestMapper.Create(), actor, actor, new FixedClock(Now), NullLogger<RecommendationService>.Instance);

        await service.RecordInstrumentViewAsync(instrumentY.Id);
        await service.RecordInstrumentViewAsync(instrumentY.Id);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(instrumentY.Id, result[0].Instrument.Id);
        Assert.Equal(0.87, result[0].Score);
        Assert.Equal(instrumentX.Id, result[1].Instrument.Id);
        Assert.Equal(0.645, result[1].Score);
        Assert.True(result[0].Instrument.IsAvailable);
        Assert.True(result[1].Instrument.IsAvailable);
    }
}

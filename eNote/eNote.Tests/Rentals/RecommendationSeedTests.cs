using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Infrastructure.Data.Seed;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Rentals;

public sealed class RecommendationSeedTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_OnSeededData_ReturnsTheDocumentedTopFive()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        await context.Database.EnsureCreatedAsync();
        await using var provider = SeedTestHarness.BuildProvider(context, Now);
        var clock = new FixedClock(Now);

        await IdentitySeed.SeedAsync(provider);
        await DevelopmentDataSeed.SeedAsync(context, clock, new RentalOptions { OverduePenaltyMultiplier = 0.5m });

        var studentUserId = await context.Users
            .Where(u => u.UserName == "student")
            .Select(u => u.Id)
            .SingleAsync();
        var student = await context.Set<Student>().SingleAsync(s => s.AppUserId == studentUserId);

        var actor = new StubCurrentActor(student: student);
        var service = new RecommendationService(context, TestMapper.Create(), actor, actor, clock, NullLogger<RecommendationService>.Instance);

        var result = await service.GetRecommendedInstrumentsAsync();

        Assert.Equal(
            ["D-28", "Stratocaster", "Precision Bass", "J-45", "Thunderbird"],
            result.Select(r => r.Instrument.Model));
        Assert.Equal([0.7033, 0.645, 0.6117, 0.545, 0.545], result.Select(r => r.Score));
        Assert.Equal(
            [
                "Na osnovu vaše historije najma (Žičani).",
                "Studenti sa sličnim izborima najma biraju ovaj instrument.",
                "Pregledali ste ovaj instrument.",
                "Sličan vašim prethodnim izborima proizvođača ili vrste."
            ],
            result[0].Reasons);
    }
}

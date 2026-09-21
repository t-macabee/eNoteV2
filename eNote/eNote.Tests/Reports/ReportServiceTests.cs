using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Infrastructure.Reports;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;

namespace eNote.Tests.Reports;

public sealed class ReportServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GenerateCourseRankingPdfAsync_ReturnsPdf_ForOwnedCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var submission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        submission.Submit("/hw.pdf", Now);
        submission.SetGrade(90);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor);

        var bytes = await service.GenerateCourseRankingPdfAsync(harness.Course.Id);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_Throws_ForLectureOfAnotherInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, otherInstructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateLectureAttendancePdfAsync(harness.Lecture.Id));
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_ReturnsPdf_WithActorStoreSet()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(harness.Context);
        var rental = RentalTestData.CreateCompletedRental(instrument, harness.Student.Id, Now);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: instrument.MusicStoreId));

        var bytes = await service.GenerateStoreRentalSummaryPdfAsync();

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_Throws_WhenStoreNotResolved()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness, harness.Instructor, new ThrowingStoreContext());

        await Assert.ThrowsAsync<StoreNotResolvedException>(() => service.GenerateStoreRentalSummaryPdfAsync());
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_ExcludesOtherStoreRentals_WhenGlobalFilterIsPermissive()
    {
        var scopedOnlyBytes = await GenerateRentalSummaryPdfAsync(includeOtherStoreRental: false);
        var withOtherStoreRentalBytes = await GenerateRentalSummaryPdfAsync(includeOtherStoreRental: true);

        // CreatePermissiveContext leaves ExplicitStoreId unset, so the EF global tenant
        // filter is inert (GetStoreId() returns null) and cannot itself exclude the other
        // store's rental. Only the explicit MusicStoreId predicate in ReportService can.
        // No PDF text-extraction library is available in this project, so an extra row is
        // asserted via byte length rather than parsed content.
        Assert.Equal(scopedOnlyBytes.Length, withOtherStoreRentalBytes.Length);
    }

    private async Task<byte[]> GenerateRentalSummaryPdfAsync(bool includeOtherStoreRental)
    {
        var context = CreatePermissiveContext();
        var harness = await AcademicTestData.SeedAsync(context, Now);

        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var store1 = new MusicStore("Music Shop A", "09-17");
        var store2 = new MusicStore("Music Shop B", "09-17");
        context.Set<MusicStore>().AddRange(store1, store2);
        await context.SaveChangesAsync();

        var instrument1 = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store1.Id);
        context.Set<Instrument>().Add(instrument1);
        await context.SaveChangesAsync();

        context.Set<InstrumentRental>().Add(RentalTestData.CreateCompletedRental(instrument1, harness.Student.Id, Now));

        if (includeOtherStoreRental)
        {
            var instrument2 = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store2.Id);
            context.Set<Instrument>().Add(instrument2);
            await context.SaveChangesAsync();

            context.Set<InstrumentRental>().Add(RentalTestData.CreateCompletedRental(instrument2, harness.Student.Id, Now));
        }

        await context.SaveChangesAsync();

        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: store1.Id));
        return await service.GenerateStoreRentalSummaryPdfAsync();
    }

    private static ENoteContext CreatePermissiveContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(storeId: 1));
    }

    private static ReportService CreateService(AcademicHarness harness, Instructor instructor, IStoreContext? stores = null)
    {
        var actor = new StubCurrentActor(instructor: instructor, student: harness.Student);
        var clock = new FixedClock(Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(harness.Context, instructor);
        var displayNames = new StubDisplayNameService();
        var ranking = new RankingService(harness.Context, actor, actor, displayNames, instructorAccess, clock);
        return new ReportService(
            harness.Context,
            clock,
            ranking,
            instructorAccess,
            actor,
            stores ?? new StubCurrentActor(storeId: 1),
            displayNames);
    }

    private sealed class ThrowingStoreContext : IStoreContext
    {
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
            throw new StoreNotResolvedException("Store not resolved.");
    }
}

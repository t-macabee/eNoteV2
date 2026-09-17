using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Infrastructure.Reports;
using eNote.Tests.TestUtils;
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
